using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Core.Serialization;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.AzureSearch;

internal class AzureSearchRequest
{
  // https://learn.microsoft.com/en-us/dotnet/api/overview/azure/search.documents-readme?view=azure-dotnet

  private const string SUGGESTER_NAME = "suggest-title";

  public Uri Uri { get; }
  public string IndexName { get; }
  public string IndexerName { get; }

  private string ApiKey { get; }

  private TimeSpan IndexingPause { get; } = TimeSpan.FromSeconds(1);

  private SearchIndexClient _searchIndexClient { get; set; }
  private SearchClient _searchClient { get; set; }

  public AzureSearchRequest(Uri uri, string apiKey, string indexName, string indexerName)
      : this(uri, apiKey, indexName, indexerName, TimeSpan.Zero) { }

  public AzureSearchRequest(Uri uri, string apiKey, string indexName, string indexerName, TimeSpan indexingPause)
  {
    this.Uri = uri;
    this.IndexName = indexName.ToLower();  // search indexes must be lower case
    this.IndexerName = indexerName;
    this.ApiKey = apiKey;
    this.IndexingPause = indexingPause;
  }

  private async Task<SearchIndexClient> SearchIndexClient()
  {
    if (this._searchIndexClient == null)
    {
      await Connect();
    }

    return _searchIndexClient;
  }

  private async Task<SearchClient> GetSearchClient()
  {
    return (await this.SearchIndexClient()).GetSearchClient(this.IndexName);
  }

  public Boolean Equals(Uri uri, string indexName, string apiKey)
  {
    return this.Uri.AbsoluteUri == uri.AbsoluteUri && this.IndexName == indexName && this.ApiKey == apiKey;
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
    JsonSerializerOptions serializerOptions = new()
    {
      DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    Azure.Core.Serialization.JsonObjectSerializer serializer = new(serializerOptions);

    SearchClientOptions options = new()
    {
      Serializer = serializer
    };

    SearchIndexClient client = new SearchIndexClient(this.Uri, new AzureKeyCredential(this.ApiKey), options);

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
    FieldBuilder builder = new();
    IList<SearchField> fields = builder.Build(typeof(AzureSearchDocument));

    SearchIndex searchIndex = new(this.IndexName, fields)
    {
      Similarity = new BM25Similarity() { B = 0.75, K1 = 1.2 }
    };

    searchIndex.Suggesters.Add(new(SUGGESTER_NAME, BuildSuggesterFields()));

    Response<SearchIndex> createIndexResponse = await client.CreateIndexAsync(searchIndex);
  }

  public async Task<Boolean> ClearIndex()
  {
    Boolean result = await this.DeleteIndex();
    
    // re-create the index
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
    SearchIndexerClient client = new(this.Uri, new AzureKeyCredential(this.ApiKey));
    var response = await client.GetIndexerNamesAsync();

    return response.Value.ToList();
  }

  public async Task<string> CreateIndexer(string key, string connectionString, string rootPath, string folder)
  {
    if (string.IsNullOrEmpty(rootPath))
    {
      rootPath = folder;
      folder = "";
    }
    PathUri path = new(rootPath, folder);
    SearchIndexerClient client = new(this.Uri, new AzureKeyCredential(this.ApiKey));

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

    // create a BLOB storage indexer
    SearchIndexer searchIndexer = new($"indexer-{key}".ToLower(), dataSource.Name, this.IndexName)
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

    FieldMapping idMapping = new("metadata_storage_path")
    {
      TargetFieldName = nameof(AzureSearchDocument.Id),
      MappingFunction = new FieldMappingFunction("base64Encode")
    };

    idMapping.MappingFunction.Parameters.Add("useHttpServerUtilityUrlTokenEncode", false);

    searchIndexer.FieldMappings.Add(idMapping);
    searchIndexer.FieldMappings.Add(new("metadata_content_type") { TargetFieldName = nameof(AzureSearchDocument.ContentType) });
    searchIndexer.FieldMappings.Add(new("metadata_storage_name") { TargetFieldName = nameof(AzureSearchDocument.Title) });

    var createIndexerResponse = await client.CreateOrUpdateIndexerAsync(searchIndexer);

    return searchIndexer.Name;
  }

  public async Task RunIndexer(string name)
  {
    SearchIndexerClient client = new(this.Uri, new AzureKeyCredential(this.ApiKey));
    var response = await client.RunIndexerAsync(name);
  }

  public async Task ResetIndexer(string name)
  {
    SearchIndexerClient client = new(this.Uri, new AzureKeyCredential(this.ApiKey));
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

    // re-create the index
    _searchIndexClient = null;
    return await this.Connect();
  }

  public async Task<IndexDocumentsResult> IndexContent(AzureSearchDocument content)
  {
    IndexDocumentsBatch<AzureSearchDocument> batch = new();
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

  public async Task<Response<SearchResults<AzureSearchDocument>>> Search(SearchQuery query)
  {
    SearchClient client = await this.GetSearchClient();

    // https://learn.microsoft.com/en-us/dotnet/api/azure.search.documents.searchoptions.querytype?view=azure-dotnet
    SearchOptions searchOptions = new()
    {
      HighlightPostTag = "<em>",
      HighlightPreTag = "</em>",
      IncludeTotalCount = true,
      QueryType = SearchQueryType.Simple,  // SearchQueryType.Semantic,
      SearchMode = query.StrictSearchTerms ? SearchMode.All : SearchMode.Any,
      Size = query.PagingSettings.PageSize,
      Filter = BuildFilter($"{BuildSiteFilter(query)}", $"{BuildRolesFilter(query)}", $"{BuildScopeFilter(query)}", $"{BuildArgsFilter(query)}"),
      Skip = query.PagingSettings.CurrentPageIndex - 1
    };

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
    return new List<string>()
    {
      nameof(AzureSearchDocument.Title),
      nameof(AzureSearchDocument.Keywords),
      nameof(AzureSearchDocument.Categories),
      nameof(AzureSearchDocument.Content)
    };
  }

  private List<string> BuildSuggesterFields()
  {
    return new List<string>()
    {
      nameof(AzureSearchDocument.Title)
    };
  }

  private List<string> BuildSelectFields()
  {
    return new List<string>()
    {
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
    };
  }

  private List<string> BuildHighlightFields(SearchQuery query)
  {
    return new List<string>()
    {
      nameof(AzureSearchDocument.Title),
      nameof(AzureSearchDocument.Summary)
    };
  }

  private List<string> BuildOrderBy(SearchQuery query)
  {
    return new List<string>()
    {
      "search.score() desc"
    };
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

