using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;

// https://learn.microsoft.com/en-us/azure/search/index-projections-concept-intro?tabs=kstore-rest

namespace Nucleus.Extensions.AzureSearch;

internal partial class AzureSearchRequest
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
  private ILogger Logger { get; }

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

  internal const string SKILL_SET_NAME = "skillset-content-vectorization";
  internal static readonly char[] TOKEN_SPLIT_CHARS = [' ', ',', ';', ':', '<', '>', '.', '!', '#', '_', '\t', '\n', '\r'];
  internal const int TOKENS_PER_PAGE = 2500;

  internal const string ID_PAGE_REGEX = ".*_pages_(?<page>[0-9]*)";

  [System.Text.RegularExpressions.GeneratedRegex(ID_PAGE_REGEX)]
  private static partial System.Text.RegularExpressions.Regex GET_CHUNK_PAGE_ID_REGEX();

  // this list is from https://learn.microsoft.com/en-us/azure/search/cognitive-search-skill-document-extraction#supported-document-formats
  // we set the indexer "included extensions" so that Azure Search doesn't try to run the skill set for documents which won't have content, to avoid warnings
  private static readonly string[] INDEXER_EXTENSIONS = [".csv", ".eml", ".epub", ".gz", ".html", ".json", ".kml", ".docx", ".doc", ".docm", ".xlsx", ".xls", ".xlsm", ".pptx", ".ppt", ".pptm", ".msg", ".xml", ".odt", ".ods", ".odp", ".pdf", ".rtf", ".xml", ".zip"];

  public AzureSearchRequest(Uri uri, string apiKey, string indexName, string indexerName, string semanticRankingConfigurationName, Boolean useVectorSearch, string azureOpenAIEndpoint, string azureOpenAIApiKey, string azureOpenAIDeploymentName, ILogger logger)
      : this(uri, apiKey, indexName, indexerName, semanticRankingConfigurationName, useVectorSearch, azureOpenAIEndpoint, azureOpenAIApiKey, azureOpenAIDeploymentName, TimeSpan.Zero, logger) { }

  public AzureSearchRequest(Uri uri, string apiKey, string indexName, string indexerName, string semanticRankingConfigurationName, Boolean useVectorSearch, string azureOpenAIEndpoint, string azureOpenAIApiKey, string azureOpenAIDeploymentName, TimeSpan indexingPause, ILogger logger)
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

    this.Logger = logger;
  }

  /// <summary>
  /// Return a client which can be used to manage indexes
  /// </summary>
  /// <returns></returns>
  private async Task<SearchIndexClient> SearchIndexClient()
  {
    if (this._searchIndexClient == null)
    {
      SearchIndexClient client = CreateIndexClient();

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

  private SearchIndexClient CreateIndexClient()
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

  

  private async Task CreateIndex(SearchIndexClient client)
  {
    List<string> vectorFields = [nameof(AzureSearchDocument.TitleVector), nameof(AzureSearchDocument.SummaryVector), nameof(AzureSearchDocument.ContentVector)];
    SearchIndex searchIndex;
    try
    {
      // index exists, update it
      searchIndex = await client.GetIndexAsync(this.IndexName);

      //FieldBuilder builder = new();
      //IList<SearchField> fields = builder.Build(typeof(AzureSearchDocument))
      //  // exclude vector fields from the default index. They are added later if vectorization 
      //  // is enabled
      //  .Where(field => field.Name == nameof(AzureSearchDocument.PageNumber))
      //  .ToList();
      //searchIndex.Fields.Add(fields.First());
    }
    catch (Azure.RequestFailedException)
    {
      // index does not exist
      searchIndex = null;
    }

    if (searchIndex == null)
    {
      FieldBuilder builder = new();
      IList<SearchField> fields = builder.Build(typeof(AzureSearchDocument))
        // exclude vector fields from the default index. They are added later if vectorization 
        // is enabled
        .Where(field => !vectorFields.Contains(field.Name))
        .ToList();

      searchIndex = new(this.IndexName, fields);
    }

    searchIndex.Similarity = new BM25Similarity() { B = 0.75, K1 = 1.2 };

    if (!searchIndex.Suggesters.Any())
    {
      searchIndex.Suggesters.Add(new(SUGGESTER_NAME, BuildSuggesterFields()));
    }

    if (!searchIndex.ScoringProfiles.Any())
    {
      ScoringProfile defaultScoringProfile = new($"scoring-profile-{this.IndexName}");

      Dictionary<string, double> weights = new()
      {
        { nameof(AzureSearchDocument.Title), 2 },
        { nameof(AzureSearchDocument.Summary), 1 },
        { nameof(AzureSearchDocument.Content), 1 },
        { nameof(AzureSearchDocument.Keywords), 1.5 },

        { nameof(AzureSearchDocument.Categories), 1.5 }
      };

      defaultScoringProfile.TextWeights = new(weights);

      searchIndex.ScoringProfiles.Add(defaultScoringProfile);

      searchIndex.DefaultScoringProfile = defaultScoringProfile.Name;
    }

    Response<SearchIndex> createIndexResponse = await client.CreateOrUpdateIndexAsync(searchIndex, true);
  }

  public async Task<Boolean> ClearIndex()
  {
    SearchIndexClient client = await this.SearchIndexClient();

    // get a copy of the index definition
    Response<SearchIndex> searchIndex = await client.GetIndexAsync(this.IndexName);

    Boolean result = await this.DeleteIndex();

    // re-create the index
    Response<SearchIndex> createIndexResponse = await client.CreateIndexAsync(searchIndex);

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
    SearchIndexClient client = await this.SearchIndexClient();
    Response<SearchIndex> response = await client.GetIndexAsync(this.IndexName);

    return response.Value.SemanticSearch?.Configurations.Select(config => config.Name).ToList() ?? [];
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
      // this code creates a text-splitting skill and embedding (vector generation) skill for the file content. Generating vectors for the 
      // title and summary fields is handled by the push feed.

      SplitSkill splitSkill = new
      (
        inputs: [ new("text") { Source = "/document/content" } ],
        outputs: [ new("textItems") { TargetName = "pages" } ]
      )
      {
        Name = "Text Splitter",
        DefaultLanguageCode = "en",
        TextSplitMode = TextSplitMode.Pages, 
        MaximumPageLength=10000, 
        MaximumPagesToTake = 0, 
        PageOverlapLength=100
      };

      AzureOpenAIEmbeddingSkill contentVectorizationSkill = new
      (
        inputs: [ new("text") { Source = "/document/pages/*" } ],
        outputs: [ new("embedding") { TargetName = "content_vector" } ]
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

      SearchIndexerSkillset indexerSkillset = new(SKILL_SET_NAME, [splitSkill, contentVectorizationSkill]);

      SearchIndexerIndexProjectionSelector projectionSelector = new
      (
        targetIndexName: this.IndexName,
        parentKeyFieldName: nameof(AzureSearchDocument.ParentId),
        sourceContext: "/document/pages/*",
        mappings:
        [
          new(nameof(AzureSearchDocument.Title)) { Source = "/document/metadata_storage_name" },
          new(nameof(AzureSearchDocument.Content)) { Source = "/document/pages/*" },
          new(nameof(AzureSearchDocument.ContentVector)) { Source = "/document/pages/*/content_vector" }
        ]);

      indexerSkillset.IndexProjection = new SearchIndexerIndexProjection(selectors: [projectionSelector]);

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
          {"indexedFileNameExtensions", string.Join(',', INDEXER_EXTENSIONS)},
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
      searchIndexer.OutputFieldMappings.Add(new("/document/pages/0/content_vector") { TargetFieldName = nameof(AzureSearchDocument.ContentVector) });
    }

    var createIndexerResponse = await client.CreateOrUpdateIndexerAsync(searchIndexer);

    return searchIndexer.Name;
  }

  public async Task<Boolean> AddVectorization()
  {
    SearchIndexClient client = await this.SearchIndexClient();

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

    // todo: add vector fields to scoring profile
    //{ nameof(AzureSearchDocument.TitleVector), 2 },
    //{ nameof(AzureSearchDocument.ContentVector), 1 },
    //{ nameof(AzureSearchDocument.SummaryVector), 1 },
    return true;
  }

  public async Task<Boolean> IsVectorizationConfigured()
  {
    SearchIndexClient client = await this.SearchIndexClient();

    SearchIndex searchIndex = await client.GetIndexAsync(this.IndexName);

    return searchIndex.VectorSearch != null;
  }

  /// <summary>
  /// Add a vector field to the index, if it is not already present.
  /// </summary>
  /// <param name="searchIndex"></param>
  /// <param name="fieldName"></param>
  private static void AddVectorField(SearchIndex searchIndex, string fieldName)
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
    SearchIndexClient client = await this.SearchIndexClient();

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

    // generate vectors for simple content types
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
        // try to fit into the token limit.  

        string[] tokens = content.Content.Split(TOKEN_SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int pages = (int)Math.Floor((decimal)tokens.Length / TOKENS_PER_PAGE) + 1;
        //int page = 0;
        string[] tokenizedContent = content.Content.Split(TOKEN_SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        //string vectorContent = string.Join(' ', tokenizedContent.Skip(page * TOKENS_PER_PAGE).Take(TOKENS_PER_PAGE));


        //System.ClientModel.ClientResult<OpenAI.Embeddings.Embedding> contentVectorResponse = await embedddingClient.GenerateEmbeddingAsync(vectorContent);
        //content.ContentVector = contentVectorResponse.Value.Vector.ToArray();

        for (int page = 0; page < pages; page++)
        {
          string vectorContent = string.Join(' ', tokenizedContent.Skip(page * TOKENS_PER_PAGE).Take(TOKENS_PER_PAGE));
          //string vectorContent = string.Join(' ', content.Content.Split(filter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(page * TOKENS_PER_PAGE).Take(TOKENS_PER_PAGE));

          AzureSearchDocument pagedDocument = new();

          CopyMetaData(content, pagedDocument);
          pagedDocument.Content = vectorContent;

          pagedDocument.Id = content.Id;
          if (page > 0)
          {
            pagedDocument.Id += $"_pages_{page}";
            pagedDocument.Title += $" [{page}]";
          }

          // only try to generate vectors if the "tokenized" content is not empty
          if (!string.IsNullOrEmpty(vectorContent))
          {
            // only try to generate vectors if the "tokenized and paged" content has been reduced in size enough to fit into the token limit. Some documents may not have enough
            // delimiter characters to work with our (very) basic tokenization method
            if (vectorContent.Length < TOKENS_PER_PAGE * 8)
            {
              try
              {
                System.ClientModel.ClientResult<OpenAI.Embeddings.Embedding> contentVectorResponse = await embedddingClient.GenerateEmbeddingAsync(vectorContent);
                pagedDocument.ContentVector = contentVectorResponse.Value.Vector.ToArray();
              }
              catch (System.ClientModel.ClientResultException)
              {
                // content is too long (more than 8191 tokens)
                this.Logger.LogWarning("A vector for content of type '{type}', url '{url}' was not generated because the content could not be tokenized.", content.Type, content.Url);
              }

              if (pagedDocument.ContentVector != null || page == 0)
              {
                batch.Actions.Add(new IndexDocumentsAction<AzureSearchDocument>(IndexActionType.MergeOrUpload, pagedDocument));
              }
              else
              {
                this.Logger.LogWarning("Skipped saving chunk for content of type '{type}', url '{url}', page {page} because a content vector could not be generated.", content.Type, content.Url, page);
              }
            }
            else
            {
              this.Logger.LogWarning("Skipped saving chunk for content of type '{type}', url '{url}', page {page} because the chunk was too large.", content.Type, content.Url, page);
            }
          }
        }
      }
    }

    SearchClient client = await this.GetSearchClient();

    if (String.IsNullOrEmpty(content.Content))
    {
      batch.Actions.Add(new IndexDocumentsAction<AzureSearchDocument>(IndexActionType.MergeOrUpload, content));

      if (this.UseVectorSearch)
      {
        // for index entries which may be handled by an Azure Search indexer, look for related index entries (documents with a ParentId set to our Id)
        // and set meta-data for those index entries
        try
        {
          Response<SearchResults<AzureSearchDocument>> relatedDocumentsResponse = client.Search<AzureSearchDocument>(BuildRelatedItemsQuery(content.Id));

          if (relatedDocumentsResponse.Value != null)
          {
            foreach (SearchResult<AzureSearchDocument> relatedDocument in relatedDocumentsResponse.Value.GetResultsAsync().ToBlockingEnumerable())
            {
              CopyMetaData(content, relatedDocument.Document);

              // extract page number (for chunks) from the Id, which is generated by the Azute Search skill set index projection. Azure
              // search starts page counts at 0, so we +1 
              System.Text.RegularExpressions.Match match = GET_CHUNK_PAGE_ID_REGEX().Match(relatedDocument.Document.Id);
              if (match.Success && int.TryParse(match.Groups["page"].Value, out int pageNumber))
              {                
                relatedDocument.Document.PageNumber = pageNumber + 1;
              }
              batch.Actions.Add(new IndexDocumentsAction<AzureSearchDocument>(IndexActionType.MergeOrUpload, relatedDocument.Document));
            }
          }
        }
        catch(Exception ex)
        {
          this.Logger.LogError(ex, "Updating meta-data for chunk entries for content of type '{type}', url '{url}'.", content.Type, content.Url);
        }
      }
    }

    if (batch.Actions.Count > 0)
    {
      Response<IndexDocumentsResult> response = await client.IndexDocumentsAsync<AzureSearchDocument>(batch);

      if (this.IndexingPause > TimeSpan.Zero)
      {
        await Task.Delay(this.IndexingPause);
      }

      return response.Value;
    }

    return null;
  }

  private static void CopyMetaData(AzureSearchDocument source, AzureSearchDocument target)
  {
    target.SiteId = source.SiteId;
    target.Url = source.Url;

    target.Title = source.Title;
    target.TitleVector = source.TitleVector;

    if (string.IsNullOrEmpty(target.Summary))
    {
      target.Summary = source.Summary;
      target.SummaryVector = source.SummaryVector;
    }

    target.Size = source.Size;
    target.ContentType = source.ContentType;
    target.Categories = source.Categories;
    target.Keywords = source.Keywords;
    target.Language = source.Language;
    target.PublishedDate = source.PublishedDate;
    target.Scope = source.Scope;
    target.SourceId = source.SourceId;
    target.Type = source.Type;
    
    target.IsSecure = source.IsSecure;
    target.Roles = target.Roles;
  }

  private static SearchOptions BuildRelatedItemsQuery(string id)
  {
    SearchOptions options = new()
    {
      IncludeTotalCount = true,
      QueryType = SearchQueryType.Simple,  
      Filter = BuildFilter($"{BuildParentIdFilter(id)}"),
      Skip = 0,
      Size = 1000
    };

    return options;
  }

  public async Task<IndexDocumentsResult> RemoveContent(AzureSearchDocument content)
  {
    SearchClient client = await this.GetSearchClient();
    Response<IndexDocumentsResult> response = await client.DeleteDocumentsAsync<AzureSearchDocument>([content]);
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
      ScoringStatistics = ScoringStatistics.Global,
      QueryType = String.IsNullOrEmpty(this.SemanticConfigurationName) ? SearchQueryType.Simple : SearchQueryType.Semantic,
      SearchMode = query.StrictSearchTerms ? SearchMode.All : SearchMode.Any,
      Size = query.PagingSettings.PageSize,
      Filter = BuildFilter($"{BuildSiteFilter(query)}", $"{BuildRolesFilter(query)}", $"{BuildScopeFilter(query)}", $"{BuildArgsFilter(query)}", $"{BuildPageNumberFilter(query)}"),
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
        searchOptions.SemanticSearch.ErrorMode = SemanticErrorMode.Partial;
      }
    }

    if (this.UseVectorSearch)
    {
      searchOptions.VectorSearch = new()
      {
        FilterMode = VectorFilterMode.PostFilter
      };
      VectorizableTextQuery vectorQuery = new(query.SearchTerm)
      {
        // https://learn.microsoft.com/en-us/azure/search/vector-search-how-to-query?tabs=query-2024-07-01%2Cbuiltin-portal#number-of-ranked-results-in-a-vector-query-response
        KNearestNeighborsCount = 10 // default is 50
      };
      vectorQuery.Fields.Add(nameof(AzureSearchDocument.TitleVector));
      vectorQuery.Fields.Add(nameof(AzureSearchDocument.SummaryVector));
      vectorQuery.Fields.Add(nameof(AzureSearchDocument.ContentVector));
      //vectorQuery.Exhaustive = true;
      vectorQuery.Weight = 2;

      // todo at some future point, as Threshold is not supported by the current version of 
      // Azure.Search.Documents
      // https://learn.microsoft.com/en-us/azure/search/vector-search-how-to-query?tabs=query-2024-07-01%2Cbuiltin-portal#set-thresholds-to-exclude-low-scoring-results-preview
      //vectorQuery.Threshold.Kind = vectorSimilarity
      //vectorQuery.Threshold.Value = 0.8

      searchOptions.VectorSearch.Queries.Add(vectorQuery);
    }

    AddRange(searchOptions.SearchFields, BuildSearchFields());
    AddRange(searchOptions.Select, BuildSelectFields());
    AddRange(searchOptions.HighlightFields, BuildHighlightFields(query));

    //if (searchOptions.QueryType != SearchQueryType.Semantic)
    //{
    //  AddRange(searchOptions.OrderBy, BuildOrderBy(query));
    //}

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
    //AddRange(searchOptions.OrderBy, BuildOrderBy(query));

    if (query.SearchTerm.Length > 100)
    {
      // Azure Search has a 100 character limit for the search term
      query.SearchTerm = query.SearchTerm[..100];
    }

    Response<SuggestResults<AzureSearchDocument>> result = await client.SuggestAsync<AzureSearchDocument>(query.SearchTerm, SUGGESTER_NAME, searchOptions);

    return result;
  }

  private static void AddRange(ICollection<string> list, IEnumerable<string> values)
  {
    foreach (string value in values)
    {
      list.Add(value);
    }
  }

  private static string BuildFilter(params string[] values)
  {
    // filter null values, add spaces in between filter expressions
    return String.Join(" and ", values.Where(value => !String.IsNullOrEmpty(value)));
  }

  private static List<string> BuildSearchFields()
  {
    return
    [
      nameof(AzureSearchDocument.Title),
      nameof(AzureSearchDocument.Summary),
      nameof(AzureSearchDocument.Content),
      nameof(AzureSearchDocument.Keywords),
      nameof(AzureSearchDocument.Categories)
    ];
  }

  private static List<string> BuildSuggesterFields()
  {
    return
    [
      nameof(AzureSearchDocument.Title)
    ];
  }

  private static List<string> BuildSelectFields()
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

  private static List<string> BuildHighlightFields(SearchQuery query)
  {
    return
    [
      nameof(AzureSearchDocument.Title),
      nameof(AzureSearchDocument.Summary),
      nameof(AzureSearchDocument.Content)
    ];
  }


  // from: https://learn.microsoft.com/en-us/azure/search/hybrid-search-overview
  // Explicit sort orders override relevanced-ranked results, so if you want similarity and BM25 relevance, omit
  // sorting in your query."
  ////private List<string> BuildOrderBy(SearchQuery query)
  ////{
  ////  return
  ////  [
  ////    "search.score() desc"
  ////  ];
  ////}

  private static string BuildSiteFilter(SearchQuery query)
  {
    // Id.ToString is required here.
    // IDE0071 "thinks" that string interpolation can be simplified by removing .ToString, but the parameter to Search.Create
    // is a FormattableString object rather than a "regular" interpolated string, and SearchFilter.Create doesn't know how to
    // deal with Guids.
#pragma warning disable IDE0071 // Simplify interpolation
    return $"{nameof(AzureSearchDocument.SiteId)} eq {SearchFilter.Create($"{query.Site.Id.ToString()}")}";
#pragma warning restore IDE0071 // Simplify interpolation
  }

  private static string BuildParentIdFilter(string parentId)
  {
    // Id.ToString is required here, SearchFilter.Create cannot handle Guids
    return $"{nameof(AzureSearchDocument.ParentId)} eq {SearchFilter.Create($"{parentId}")}";
  }

  private static string BuildPageNumberFilter(SearchQuery query)
  {
    // We exclude "page 0" from search results, because it is a repeat of the main document (index entry)
    return $"{nameof(AzureSearchDocument.PageNumber)} ne 1";
  }

  private static string BuildRolesFilter(SearchQuery query)
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

  private static string BuildScopeFilter(SearchQuery query)
  {
    string result = "";

    if (query.IncludedScopes.Count != 0)
    {
      string values = SearchFilter.Create($"{String.Join(",", query.IncludedScopes.Select(scope => scope))}");
      result += $"search.in({nameof(AzureSearchDocument.Scope)}, {values}, ',')";
    }

    if (query.ExcludedScopes.Count != 0)
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

  private static string BuildArgsFilter(SearchQuery query)
  {
    // future use
    return null;
  }
}

