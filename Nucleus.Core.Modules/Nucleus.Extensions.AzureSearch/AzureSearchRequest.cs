using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;
using Microsoft.ML.Tokenizers;
using System.Text;
using HtmlAgilityPack;
using Nucleus.ViewFeatures;

namespace Nucleus.Extensions.AzureSearch;

internal class AzureSearchRequest
{
  // https://learn.microsoft.com/en-us/dotnet/api/overview/azure/search.documents-readme?view=azure-dotnet

  private const string SUGGESTER_NAME = "suggest-title";

  public Uri Uri { get; }
  public string IndexName { get; }
  public string IndexerName { get; }
  public string SemanticConfigurationName { get; }
  public Boolean UseVectorSearch { get; }
  public string AzureOpenAIEndpoint { get; }
  public string AzureOpenAIApiKey { get; }
  public string AzureOpenAIDeploymentName { get; }

  private string AzureSearchApiKey { get; }

  private TimeSpan IndexingPause { get; } = TimeSpan.FromSeconds(1);

  private SearchIndexClient _searchIndexClient { get; set; }
  private SearchClient _searchClient { get; set; }

  internal const string VECTORIZER_NAME = "openai-vectorizer";
  internal const string VECTORIZER_MODEL_NAME = "text-embedding-ada-002";

  internal const string VECTOR_HNSW_PROFILE = "vector-profile-hnsw";
  internal const string VECTOR_HNSW_CONFIG_NAME = "vector-config-hnsw";

  internal const string VECTOR_EXHASUTIVEKNN_PROFILE = "vector-profile-exhaustive-knn";
  internal const string VECTOR_EXHAUSTIVEKNN_CONFIG = "vector-config-exhaustive-knn";

  internal const int VECTOR_DIMENSIONS = 1536;

  internal const string SKILL_SET_NAME = "skillset-content-extraction";
  internal static readonly char[] TOKEN_SPLIT_CHARS = [' ', '\t', '\n', '\r'];
  internal const int TOKENS_PER_PAGE = 2500;

  public AzureSearchRequest(Uri uri, string apiKey, string indexName, string indexerName, string semanticRankingConfigurationName, Boolean useVectorSearch, string azureOpenAIEndpoint, string azureOpenAIApiKey, string azureOpenAIDeploymentName)
      : this(uri, apiKey, indexName, indexerName, semanticRankingConfigurationName, useVectorSearch, azureOpenAIEndpoint, azureOpenAIApiKey, azureOpenAIDeploymentName, TimeSpan.Zero) { }

  public AzureSearchRequest(Uri uri, string apiKey, string indexName, string indexerName, string semanticRankingConfigurationName, Boolean useVectorSearch, string azureOpenAIEndpoint, string azureOpenAIApiKey, string azureOpenAIDeploymentName, TimeSpan indexingPause)
  {
    this.Uri = uri;
    this.IndexName = indexName.ToLower();  // search indexes must be lower case
    this.IndexerName = indexerName;
    this.SemanticConfigurationName = semanticRankingConfigurationName;
    this.UseVectorSearch = useVectorSearch;
    this.AzureSearchApiKey = apiKey;
    this.IndexingPause = indexingPause;

    this.AzureOpenAIEndpoint = azureOpenAIEndpoint;
    this.AzureOpenAIApiKey = azureOpenAIApiKey;
    this.AzureOpenAIDeploymentName = azureOpenAIDeploymentName;
  }

  /// <summary>
  /// Return a client which can be used to manage indexes
  /// </summary>
  /// <returns></returns>
  private async Task<SearchIndexClient> SearchIndexClient()
  {
    if (this._searchIndexClient == null)
    {
      await Connect();
    }

    return _searchIndexClient;
  }

  /// <summary>
  /// Return a client which can be used to retrieve results from (search) indexes
  /// </summary>
  /// <returns></returns>
  private async Task<SearchClient> GetSearchClient()
  {
    if (_searchClient == null)
    {
      _searchClient = (await this.SearchIndexClient()).GetSearchClient(this.IndexName);
    }

    return _searchClient;
  }

  private SearchIndexClient GetIndexClient()
  {
    JsonSerializerOptions serializerOptions = new()
    {
      DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    Azure.Core.Serialization.JsonObjectSerializer serializer = new(serializerOptions);

    SearchClientOptions options = new()
    {
      Serializer = serializer
    };

    return new SearchIndexClient(this.Uri, new AzureKeyCredential(this.AzureSearchApiKey), options);
  }

  public Boolean Equals(Uri uri, string indexName, string apiKey)
  {
    return this.Uri.AbsoluteUri == uri.AbsoluteUri && this.IndexName == indexName && this.AzureSearchApiKey == apiKey;
  }

  public async Task<Boolean> CanConnect(Site site)
  {
    if (site == null)
    {
      throw new NullReferenceException("site must not be null.");
    }

    ConfigSettings settings = new(site);
    try
    {
      Response<SearchIndexStatistics> result = await (await this.SearchIndexClient()).GetIndexStatisticsAsync(settings.IndexName);
      return true;
    }
    catch (Exception)
    {
      return false;
    }
  }

  public async Task<bool> Connect()
  {
    SearchIndexClient client = GetIndexClient();

    // check index
    try
    {
      Response<SearchIndex> indexResponse = await client.GetIndexAsync(this.IndexName);
    }
    catch (Azure.RequestFailedException ex)
    {
      if (ex.Status == 404)
      {
        // index not found: create it
        await this.CreateIndex(client);
      }
    }

    this._searchIndexClient = client;
    return true;
  }

  private async Task CreateIndex(SearchIndexClient client)
  {
    List<string> vectorFields = [nameof(AzureSearchDocument.TitleVector), nameof(AzureSearchDocument.SummaryVector), nameof(AzureSearchDocument.ContentVector)];

    FieldBuilder builder = new();
    IList<SearchField> fields = builder.Build(typeof(AzureSearchDocument))
      // exclude vector fields from default index. They are added later if vectorization is enabled
      .Where(field => !vectorFields.Contains(field.Name))
      .ToList();

    SearchIndex searchIndex = new(this.IndexName, fields)
    {
      Similarity = new BM25Similarity() { B = 0.75, K1 = 1.2 }
    };

    searchIndex.Suggesters.Add(new(SUGGESTER_NAME, BuildSuggesterFields()));

    Response<SearchIndex> createIndexResponse = await client.CreateIndexAsync(searchIndex);
  }

  public async Task<Boolean> ClearIndex()
  {
    SearchIndexClient client = await this.SearchIndexClient();

    // get a copy of the index definition
    Response<SearchIndex> searchIndex = await client.GetIndexAsync(this.IndexName);

    Boolean result = await this.DeleteIndex();

    // re-create the index
    Response<SearchIndex> createIndexResponse = await client.CreateIndexAsync(searchIndex);

    await this.Connect();

    // reset the Azure search indexer
    if (!string.IsNullOrEmpty(this.IndexerName))
    {
      await this.ResetIndexer(this.IndexerName);
    }

    return result;
  }

  public async Task<List<string>> ListIndexers()
  {
    SearchIndexerClient client = new(this.Uri, new AzureKeyCredential(this.AzureSearchApiKey));
    var response = await client.GetIndexerNamesAsync();

    return response.Value.ToList();
  }

  public async Task<List<string>> ListSemanticRankingConfigurations()
  {
    SearchIndexClient client = this.GetIndexClient();
    Response<SearchIndex> response = await client.GetIndexAsync(this.IndexName);

    return response.Value.SemanticSearch?.Configurations.Select(config => config.Name).ToList() ?? new();
  }

  public async Task<string> CreateIndexer(string key, string connectionString, string rootPath, string folder)
  {
    // https://learn.microsoft.com/en-us/azure/search/index-projections-concept-intro?tabs=kstore-rest

    if (string.IsNullOrEmpty(rootPath))
    {
      rootPath = folder;
      folder = "";
    }
    PathUri path = new(rootPath, folder);
    SearchIndexerClient client = new(this.Uri, new AzureKeyCredential(this.AzureSearchApiKey));

    // create or update a data source
    SearchIndexerDataSourceConnection dataSource = new
    (
      $"data-source-{key}".ToLower(),
      SearchIndexerDataSourceType.AzureBlob,
      connectionString,
      new SearchIndexerDataContainer(path.ContainerName) { Query = path.RelativePath }
    )
    {
      Description = $"Auto-generated indexer data source for the {this.IndexName} index. This data source was created by Nucleus."
    };

    var createDataSourceResponse = await client.CreateOrUpdateDataSourceConnectionAsync(dataSource);

    // create skill sets, if vectorization is enabled
    if (this.UseVectorSearch)
    {
      SplitSkill splitSkill = new 
      (
        new List<InputFieldMappingEntry>()
        {
          new("text") { Source = "/document/content" }//,
          //new("languageCode") { Source = "/document/Language" },
        },
        new List<OutputFieldMappingEntry>()
        {
          new("textItems") { TargetName = "pages" }
        }
      )
      {
        Name = "Text Splitter",
        DefaultLanguageCode = "en",
        TextSplitMode = TextSplitMode.Pages//,
         //Context = "/document"
      };

      AzureOpenAIEmbeddingSkill embeddingSkill = new
      (
        new List<InputFieldMappingEntry>()
        {
          new("text") { Source = "/document/pages/*" }
        },
        new List<OutputFieldMappingEntry>()
        {
          new("embedding") { TargetName = "content_vector" }
        }
      )
      {
        Name = "Vector Embedding - Content",
        ResourceUri = new(this.AzureOpenAIEndpoint),
        ApiKey = this.AzureOpenAIApiKey,
        DeploymentName = this.AzureOpenAIDeploymentName,
        Context = "/document/pages/*",
        Dimensions = VECTOR_DIMENSIONS,
        ModelName = VECTORIZER_MODEL_NAME
      };

      SearchIndexerSkillset indexerSkillset = new(SKILL_SET_NAME, [splitSkill, embeddingSkill]);
     
      Response<SearchIndexerSkillset> skillsetResponse = await client.CreateOrUpdateSkillsetAsync(indexerSkillset);
    }


    // create a BLOB storage indexer
    SearchIndexer searchIndexer = new($"indexer-{key}-{this.IndexName}".ToLower(), dataSource.Name, this.IndexName)
    {
      Description = $"Auto-generated storage indexer for the {this.IndexName} index. This indexer was created by Nucleus.",

      Parameters = new()
      {
        IndexingParametersConfiguration = new()
        {
          {"indexedFileNameExtensions", ""},
          {"excludedFileNameExtensions", ""},
          {"failOnUnsupportedContentType", false},
          {"indexStorageMetadataOnlyForOversizedDocuments", false},
          {"dataToExtract", "contentAndMetadata"},
          {"parsingMode", "default"},
        }
      }
    };

    if (this.UseVectorSearch)
    {
      searchIndexer.SkillsetName = SKILL_SET_NAME;
    }

    FieldMapping idMapping = new("metadata_storage_path")
    {
      TargetFieldName = nameof(AzureSearchDocument.Id),
      MappingFunction = new FieldMappingFunction("base64Encode")
    };

    idMapping.MappingFunction.Parameters.Add("useHttpServerUtilityUrlTokenEncode", false);

    searchIndexer.FieldMappings.Add(idMapping);
    searchIndexer.FieldMappings.Add(new("metadata_content_type") { TargetFieldName = nameof(AzureSearchDocument.ContentType) });
    searchIndexer.FieldMappings.Add(new("metadata_storage_name") { TargetFieldName = nameof(AzureSearchDocument.Title) });

    if (this.UseVectorSearch)
    {
      // pageNumber probably doesn't work
      //searchIndexer.OutputFieldMappings.Add(new("/document/pages/*/pageNumber") { TargetFieldName = $"{nameof(AzureSearchDocument.Pages)}/{nameof(AzureSearchDocumentPage.PageNumber)}" });
      //searchIndexer.OutputFieldMappings.Add(new("/document/pages/*") { TargetFieldName = $"{nameof(AzureSearchDocument.Pages)}/{nameof(AzureSearchDocumentPage.ContentVector)}" });
      //searchIndexer.OutputFieldMappings.Add(new("/document/pages/*/content_vector") { TargetFieldName = $"{nameof(AzureSearchDocument.Pages)}/{nameof(AzureSearchDocumentPage.ContentVector)}" });

      searchIndexer.OutputFieldMappings.Add(new("/document/pages/0/content_vector") { TargetFieldName = nameof(AzureSearchDocument.ContentVector) });
    }

    var createIndexerResponse = await client.CreateOrUpdateIndexerAsync(searchIndexer);

    return searchIndexer.Name;
  }

  public async Task<Boolean> AddVectorization()
  {
    SearchIndexClient client = GetIndexClient();

    SearchIndex searchIndex = await client.GetIndexAsync(this.IndexName);

    searchIndex.VectorSearch = new()
    {
      Profiles =
      {
        new VectorSearchProfile(VECTOR_HNSW_PROFILE, VECTOR_HNSW_CONFIG_NAME)
        {
          VectorizerName = VECTORIZER_NAME,
        },
        new VectorSearchProfile(VECTOR_EXHASUTIVEKNN_PROFILE, VECTOR_EXHAUSTIVEKNN_CONFIG)
      },
      Algorithms =
      {
          new HnswAlgorithmConfiguration(VECTOR_HNSW_CONFIG_NAME),
          new ExhaustiveKnnAlgorithmConfiguration(VECTOR_EXHAUSTIVEKNN_CONFIG)
      },
      Vectorizers =
      {
        new AzureOpenAIVectorizer(VECTORIZER_NAME)
        {
          Parameters = new()
          {
            ResourceUri = new(this.AzureOpenAIEndpoint),
            ApiKey = this.AzureOpenAIApiKey,
            DeploymentName = this.AzureOpenAIDeploymentName,
            ModelName = VECTORIZER_MODEL_NAME
          }
        }
      }
    };

    AddVectorField(searchIndex, nameof(AzureSearchDocument.TitleVector));
    AddVectorField(searchIndex, nameof(AzureSearchDocument.SummaryVector));
    AddVectorField(searchIndex, nameof(AzureSearchDocument.ContentVector));

    Response<SearchIndex> createIndexResponse = await client.CreateOrUpdateIndexAsync(searchIndex, true);

    return true;
  }

  public async Task<Boolean> IsVectorizationConfigured()
  {
    SearchIndexClient client = GetIndexClient();

    SearchIndex searchIndex = await client.GetIndexAsync(this.IndexName);

    return searchIndex.VectorSearch != null;
  }

  private void AddVectorField(SearchIndex searchIndex, string fieldName)
  {
    if (!searchIndex.Fields.Any(field => field.Name == fieldName))
    {
      searchIndex.Fields.Add(new(fieldName, SearchFieldDataType.Collection(SearchFieldDataType.Single))
      {
        IsSearchable = true,
        VectorSearchDimensions = VECTOR_DIMENSIONS,
        VectorSearchProfileName = VECTOR_HNSW_PROFILE
      });
    }
  }

  public async Task<string> AddSemanticRanking()
  {
    SearchIndexClient client = GetIndexClient();
    string semanticConfigurationName = $"semantic-config-{this.IndexName}";

    SearchIndex searchIndex = await client.GetIndexAsync(this.IndexName);
    searchIndex.SemanticSearch = new()
    {
      Configurations =
        {
          new SemanticConfiguration(semanticConfigurationName, new()
          {
            TitleField = new SemanticField(nameof(AzureSearchDocument.Title)),
            ContentFields =
            {
              new SemanticField(nameof(AzureSearchDocument.Summary)),
              new SemanticField(nameof(AzureSearchDocument.Content))
            },
            KeywordsFields =
            {
              new SemanticField(nameof(AzureSearchDocument.Keywords))
            }
          })
        }
    };

    Response<SearchIndex> createIndexResponse = await client.CreateOrUpdateIndexAsync(searchIndex, true);

    return semanticConfigurationName;
  }

  public async Task RunIndexer(string name)
  {
    SearchIndexerClient client = new(this.Uri, new AzureKeyCredential(this.AzureSearchApiKey));
    var response = await client.RunIndexerAsync(name);
  }

  public async Task ResetIndexer(string name)
  {
    SearchIndexerClient client = new(this.Uri, new AzureKeyCredential(this.AzureSearchApiKey));
    var response = await client.ResetIndexerAsync(name);
  }

  public async Task<SearchIndexStatistics> GetIndexSettings()
  {
    SearchIndexClient client = await this.SearchIndexClient();

    // check index
    Response<SearchIndexStatistics> indexResponse = await client.GetIndexStatisticsAsync(this.IndexName);

    return indexResponse.Value;
  }

  public async Task<Boolean> DeleteIndex()
  {
    SearchIndexClient client = await this.SearchIndexClient();
    await client.GetIndexAsync(this.IndexName);
    await client.DeleteIndexAsync(this.IndexName);

    return true;
  }

  public async Task<IndexDocumentsResult> IndexContent(AzureSearchDocument content)
  {
    IndexDocumentsBatch<AzureSearchDocument> batch = new();

    // generate vectors
    if (this.UseVectorSearch)
    {
      Azure.AI.OpenAI.AzureOpenAIClient aiClient = new(new(this.AzureOpenAIEndpoint), new System.ClientModel.ApiKeyCredential(this.AzureOpenAIApiKey));
      
      OpenAI.Embeddings.EmbeddingClient embedddingClient = aiClient.GetEmbeddingClient(this.AzureOpenAIDeploymentName);

      if (!String.IsNullOrEmpty(content.Title))
      {
        System.ClientModel.ClientResult<OpenAI.Embeddings.Embedding> titleVectorResponse = await embedddingClient.GenerateEmbeddingAsync(content.Title);
        content.TitleVector = titleVectorResponse.Value.Vector.ToArray();
      }

      if (!String.IsNullOrEmpty(content.Summary))
      {
        System.ClientModel.ClientResult<OpenAI.Embeddings.Embedding> summaryVectorResponse = await embedddingClient.GenerateEmbeddingAsync(content.Summary);
        content.SummaryVector = summaryVectorResponse.Value.Vector.ToArray();
      }

      if (!String.IsNullOrEmpty(content.Content))
      {
        try
        {
          // try to fit into the token limit.  
          
          string[] tokens = content.Content.Split(TOKEN_SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
          int pages = tokens.Length / TOKENS_PER_PAGE;
          int page = 0;
          string vectorContent = string.Join(' ', content.Content.Split(TOKEN_SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(page * TOKENS_PER_PAGE).Take(TOKENS_PER_PAGE));

          System.ClientModel.ClientResult<OpenAI.Embeddings.Embedding> contentVectorResponse = await embedddingClient.GenerateEmbeddingAsync(vectorContent);
          content.ContentVector = contentVectorResponse.Value.Vector.ToArray();
          //for (int page=0; page < pages; page++) 
          //{ 
          //  string vectorContent = string.Join(' ', content.Content.Split(filter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(page* TOKENS_PER_PAGE).Take(TOKENS_PER_PAGE));

          //  System.ClientModel.ClientResult<OpenAI.Embeddings.Embedding> contentVectorResponse = await embedddingClient.GenerateEmbeddingAsync(vectorContent);
          //  content.Pages.Add(new() { Content = vectorContent, ContentVector = contentVectorResponse.Value.Vector.ToArray(), PageNumber = page+1 });
          //}; 

        }
        catch (System.ClientModel.ClientResultException)
        {
          // content is too long (more than 8191 tokens)
          
        }
      }
    }

    batch.Actions.Add(new IndexDocumentsAction<AzureSearchDocument>(IndexActionType.MergeOrUpload, content));

    // SearchIndexingBufferedSender: https://learn.microsoft.com/en-us/dotnet/api/azure.search.documents.searchindexingbufferedsender-1?view=azure-dotnet

    SearchClient client = await this.GetSearchClient();

    Response<IndexDocumentsResult> response = await client.IndexDocumentsAsync<AzureSearchDocument>(batch);
    return response.Value;
  }

  public async Task<IndexDocumentsResult> RemoveContent(AzureSearchDocument content)
  {
    SearchClient client = await this.GetSearchClient();
    Response<IndexDocumentsResult> response = await client.DeleteDocumentsAsync<AzureSearchDocument>(new List<AzureSearchDocument>() { content }, new() { });
    return response.Value;
  }

  public async Task<AzureSearchDocument> GetContentByKey(string key)
  {
    SearchClient client = await this.GetSearchClient();
    AzureSearchDocument response = await client.GetDocumentAsync<AzureSearchDocument>(key);
    return response;
  }

  public async Task<Response<SearchResults<AzureSearchDocument>>> Search(SearchQuery query)
  {
    SearchClient client = await this.GetSearchClient();

    // https://learn.microsoft.com/en-us/dotnet/api/azure.search.documents.searchoptions.querytype?view=azure-dotnet
    SearchOptions searchOptions = new()
    {
      HighlightPreTag = "<em>",
      HighlightPostTag = "</em>",
      IncludeTotalCount = true,
      QueryType = String.IsNullOrEmpty(this.SemanticConfigurationName) ? SearchQueryType.Simple : SearchQueryType.Semantic,
      SearchMode = query.StrictSearchTerms ? SearchMode.All : SearchMode.Any,
      Size = query.PagingSettings.PageSize,
      Filter = BuildFilter($"{BuildSiteFilter(query)}", $"{BuildRolesFilter(query)}", $"{BuildScopeFilter(query)}", $"{BuildArgsFilter(query)}"),
      Skip = query.PagingSettings.CurrentPageIndex - 1
    };

    if (!String.IsNullOrEmpty(this.SemanticConfigurationName))
    {
      searchOptions.SemanticSearch = new()
      {
        SemanticConfigurationName = this.SemanticConfigurationName
      };

      if (searchOptions.QueryType == SearchQueryType.Semantic)
      {
        searchOptions.SemanticSearch.QueryCaption = new(QueryCaptionType.Extractive);
        searchOptions.SemanticSearch.QueryAnswer = new(QueryAnswerType.Extractive) { Count = 3 };
        searchOptions.SemanticSearch.ErrorMode = SemanticErrorMode.Fail;
      }
    }

    if (this.UseVectorSearch)
    {
      searchOptions.VectorSearch = new()
      {
        FilterMode = VectorFilterMode.PostFilter
      };
      VectorizableTextQuery vectorQuery = new(query.SearchTerm);

      vectorQuery.Fields.Add(nameof(AzureSearchDocument.TitleVector));
      vectorQuery.Fields.Add(nameof(AzureSearchDocument.SummaryVector));
      vectorQuery.Fields.Add(nameof(AzureSearchDocument.ContentVector));

      searchOptions.VectorSearch.Queries.Add(vectorQuery);
    }

    AddRange(searchOptions.SearchFields, BuildSearchFields());
    AddRange(searchOptions.Select, BuildSelectFields());
    AddRange(searchOptions.HighlightFields, BuildHighlightFields(query));

    if (searchOptions.QueryType != SearchQueryType.Semantic)
    {
      AddRange(searchOptions.OrderBy, BuildOrderBy(query));
    }

    if (query.SearchTerm.Length > 100)
    {
      // Azure Search has a 100 character limit for the search term
      query.SearchTerm = query.SearchTerm[..100];
    }

    return await client.SearchAsync<AzureSearchDocument>(query.SearchTerm, searchOptions);
  }

  public async Task<Response<SuggestResults<AzureSearchDocument>>> Suggest(SearchQuery query)
  {
    SearchClient client = await this.GetSearchClient();

    SuggestOptions searchOptions = new()
    {
      Size = query.PagingSettings.PageSize,
      Filter = BuildFilter(BuildSiteFilter(query), BuildRolesFilter(query), BuildScopeFilter(query), BuildArgsFilter(query)),
      UseFuzzyMatching = true
    };

    AddRange(searchOptions.SearchFields, BuildSuggesterFields());
    AddRange(searchOptions.Select, BuildSelectFields());
    AddRange(searchOptions.OrderBy, BuildOrderBy(query));

    if (query.SearchTerm.Length > 100)
    {
      // Azure Search has a 100 character limit for the search term
      query.SearchTerm = query.SearchTerm[..100];
    }

    Response<SuggestResults<AzureSearchDocument>> result = await client.SuggestAsync<AzureSearchDocument>(query.SearchTerm, SUGGESTER_NAME, searchOptions);

    return result;
  }

  private void AddRange(ICollection<string> list, IEnumerable<string> values)
  {
    foreach (string value in values)
    {
      list.Add(value);
    }
  }

  private string BuildFilter(params string[] values)
  {
    // filter null values, add spaces in between filter expressions
    return String.Join(" and ", values.Where(value => !String.IsNullOrEmpty(value)));
  }

  private List<string> BuildSearchFields()
  {
    return
    [
      nameof(AzureSearchDocument.Title),
      nameof(AzureSearchDocument.Content),
      nameof(AzureSearchDocument.Keywords),
      nameof(AzureSearchDocument.Categories)
    ];
  }

  private List<string> BuildSuggesterFields()
  {
    return
    [
      nameof(AzureSearchDocument.Title)
    ];
  }

  private List<string> BuildSelectFields()
  {
    return
    [
      nameof(AzureSearchDocument.SiteId),
      nameof(AzureSearchDocument.Url),
      nameof(AzureSearchDocument.Title),
      nameof(AzureSearchDocument.Summary),
      nameof(AzureSearchDocument.Scope),
      nameof(AzureSearchDocument.Type),
      nameof(AzureSearchDocument.SourceId),
      nameof(AzureSearchDocument.ContentType),
      nameof(AzureSearchDocument.PublishedDate),
      nameof(AzureSearchDocument.Size),
      nameof(AzureSearchDocument.Keywords),
      nameof(AzureSearchDocument.Categories),
      nameof(AzureSearchDocument.Roles)
    ];
  }

  private List<string> BuildHighlightFields(SearchQuery query)
  {
    return
    [
      nameof(AzureSearchDocument.Title),
      nameof(AzureSearchDocument.Summary),
      nameof(AzureSearchDocument.Content)
    ];
  }

  private List<string> BuildOrderBy(SearchQuery query)
  {
    return
    [
      "search.score() desc"
    ];
  }

  private string BuildSiteFilter(SearchQuery query)
  {
    // Id.ToString is required here, SearchFilter.Create cannot handle Guids
    return $"{nameof(AzureSearchDocument.SiteId)} eq {SearchFilter.Create($"{query.Site.Id.ToString()}")}";
  }

  private string BuildRolesFilter(SearchQuery query)
  {
    // https://learn.microsoft.com/en-us/azure/search/search-query-odata-search-in-function
    if (query.Roles?.Any() == true)
    {
      // Id.ToString is required here, SearchFilter.Create cannot handle Guids
      string values = SearchFilter.Create($"{String.Join(",", query.Roles.Select(role => role.Id.ToString()))}");
      return $"({nameof(AzureSearchDocument.Roles)}/any(g: search.in(g, {values})) or {nameof(AzureSearchDocument.IsSecure)} eq false)";
    }
    else
    {
      return null;
    }
  }

  private string BuildScopeFilter(SearchQuery query)
  {
    string result = "";

    if (query.IncludedScopes.Any())
    {
      string values = SearchFilter.Create($"{String.Join(",", query.IncludedScopes.Select(scope => scope))}");
      result += $"search.in({nameof(AzureSearchDocument.Scope)}, {values}, ',')";
    }

    if (query.ExcludedScopes.Any())
    {
      string values = SearchFilter.Create($"{String.Join(",", query.ExcludedScopes.Select(scope => scope))}");
      string result2 = $"(search.in({nameof(AzureSearchDocument.Scope)}, {values}, ',') eq false)";

      if (!String.IsNullOrEmpty(result))
      {
        result = $"({result} and {result2})";
      }
      else
      {
        result = result2;
      }
    }

    return String.IsNullOrEmpty(result) ? null : result;
  }

  private string BuildArgsFilter(SearchQuery query)
  {
    // future use
    return null;
  }
}

