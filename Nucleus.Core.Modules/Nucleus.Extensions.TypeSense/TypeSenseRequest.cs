using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Typesense;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;

namespace Nucleus.Extensions.TypeSense;

internal class TypeSenseRequest
{
  System.Net.Http.IHttpClientFactory HttpClientFactory { get; }
  public Uri Uri { get; }
  public string IndexName { get; }

  private string ApiKey { get; }

  private TimeSpan IndexingPause { get; } = TimeSpan.FromSeconds(1);

  private Typesense.ITypesenseClient _client { get; set; }

  public string DebugInformation { get; internal set; }

  public TypeSenseRequest(System.Net.Http.IHttpClientFactory httpClientFactory, Uri uri, string indexName, string apiKey)
    : this(httpClientFactory, uri, indexName, apiKey, TimeSpan.Zero) { }

  public TypeSenseRequest(System.Net.Http.IHttpClientFactory httpClientFactory, Uri uri, string indexName, string apiKey, TimeSpan indexingPause)
  {
    this.HttpClientFactory = httpClientFactory;
    this.Uri = uri;
    this.IndexName = indexName;
    this.ApiKey = apiKey;
    this.IndexingPause = indexingPause;
  }

  public Boolean Equals(Uri uri, string indexName, string apiKey)
  {
    return this.Uri.AbsoluteUri != uri.AbsoluteUri || this.IndexName != indexName || this.ApiKey != apiKey;
  }

  public async Task<ITypesenseClient> GetClient()
  {
    if (this._client == null)
    {
      if (!await Connect())
      {
        throw new InvalidOperationException(this.DebugInformation);
      }
    }
    return this._client;
  }

  public async Task<bool> Connect()
  {
    Boolean pingResponse;

    List<Typesense.Setup.Node> nodes = [new(this.Uri.Host, this.Uri.Port.ToString(), this.Uri.Scheme)];
    Typesense.Setup.Config config = new(nodes, this.ApiKey);
    //config.JsonSerializerOptions = new() 
    //{ 
    //   DefaultIgnoreCondition= System.Text.Json.Serialization.JsonIgnoreCondition.Never,
    //  PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.
    //};

    IOptions<Typesense.Setup.Config> options = Options.Create(config);

    ITypesenseClient client = new TypesenseClient(options, this.HttpClientFactory.CreateClient(nameof(TypeSenseRequest)));

    pingResponse = (await client.RetrieveHealth()).Ok;
    if (pingResponse)
    {
      // Create index
      if (!(await client.RetrieveCollections()).Any(collection => collection.Name == this.IndexName))
      {
        if (!await CreateIndex(client))
        {
          return false;
        }
      }
    }
    else
    {
      this.DebugInformation = "Ping failed";
      return false;
    }

    this._client = client;
    return true;
  }

  private async Task<bool> CreateIndex(ITypesenseClient client)
  {
    Schema schema = new(
      this.IndexName,
      new List<Field>
      {
        new Field(CamelCase(nameof(TypeSenseDocument.Id)), FieldType.String, false),
        new Field(CamelCase(nameof(TypeSenseDocument.ParentId)), FieldType.String, false, true),
        new Field(CamelCase(nameof(TypeSenseDocument.SiteId)), FieldType.String, false),
        new Field(CamelCase(nameof(TypeSenseDocument.Url)), FieldType.String, false),
        new Field(CamelCase(nameof(TypeSenseDocument.PageNumber)), FieldType.Int32, false, true),
        new Field(CamelCase(nameof(TypeSenseDocument.Title)), FieldType.String, false, true),
        new Field(CamelCase(nameof(TypeSenseDocument.TitleVector)), FieldType.FloatArray, false, true)
        {
          Embed = new([CamelCase(nameof(TypeSenseDocument.Title))],new("ts/gte-large") )
        },
        new Field(CamelCase(nameof(TypeSenseDocument.Content)), FieldType.String, false, true),
        new Field(CamelCase(nameof(TypeSenseDocument.ContentVector)), FieldType.FloatArray, false, true)
        {
          Embed = new([CamelCase(nameof(TypeSenseDocument.Content))], new("ts/gte-large") )
        },
        new Field(CamelCase(nameof(TypeSenseDocument.Summary)), FieldType.String, false, true),
        new Field(CamelCase(nameof(TypeSenseDocument.SummaryVector)), FieldType.FloatArray, false, true)
        {
          Embed = new([CamelCase(nameof(TypeSenseDocument.Summary))], new("ts/gte-large") )
        },

        new Field(CamelCase(nameof(TypeSenseDocument.Categories)), FieldType.StringArray, false, true),
        new Field(CamelCase(nameof(TypeSenseDocument.Keywords)), FieldType.StringArray, false, true),
        new Field(CamelCase(nameof(TypeSenseDocument.Scope)), FieldType.String, false, true),
        new Field(CamelCase(nameof(TypeSenseDocument.SourceId)), FieldType.String, false, true),

        new Field(CamelCase(nameof(TypeSenseDocument.ContentType)), FieldType.String, false, true),
        new Field(CamelCase(nameof(TypeSenseDocument.PublishedDate)), FieldType.String, false, true),
        new Field(CamelCase(nameof(TypeSenseDocument.Type)), FieldType.String, false, true),
        new Field(CamelCase(nameof(TypeSenseDocument.Size)), FieldType.Int64, false, true),

        new Field(CamelCase(nameof(TypeSenseDocument.IsSecure)), FieldType.Bool, false, true),
        new Field(CamelCase(nameof(TypeSenseDocument.Roles)), FieldType.StringArray, false, true)
      });

    try
    {
      CollectionResponse createCollectionResponse = await client.CreateCollection(schema);
      return true;
    }
    catch (Exception ex)
    {
      this.DebugInformation = ex.Message;
      return false;
    }
  }

  public async Task<long> CountIndex()
  {
    ITypesenseClient client = await GetClient();
    return (await client.RetrieveCollection(this.IndexName)).NumberOfDocuments;
  }

  public async Task<bool> ClearIndex()
  {
    ITypesenseClient client = await GetClient();
    //await client.DeleteDocuments(this.IndexName, $"{{'filter_by': '{nameof(TypeSenseDocument.Id)}}}:>''}}");
    if (await DeleteIndex())
    {
      await CreateIndex(client);
    }
    return true;
  }


  public async Task<bool> DeleteIndex()
  {
    ITypesenseClient client = await GetClient();

    try
    {
      CollectionResponse objIndexResponse = await client.DeleteCollection(this.IndexName);
      return true;
    }
    catch (Exception ex)
    {
      this.DebugInformation = ex.Message;
      return false;
    }
  }


  public async Task<CollectionResponse> GetIndexSettings()
  {
    ITypesenseClient client = await GetClient();
    return await client.RetrieveCollection(this.IndexName);
  }

  public async Task<List<ImportResponse>> IndexContent(IEnumerable<TypeSenseDocument> contents)
  {
    ITypesenseClient client = await GetClient();

    List<ImportResponse> indexResponse = await client.ImportDocuments<TypeSenseDocument>(this.IndexName, contents, contents.Count(), ImportType.Upsert);

    foreach (ImportResponse response in indexResponse)
    {
      if (!response.Success)
      {
        throw new InvalidOperationException(response.Error);
      }
    }

    if (this.IndexingPause > TimeSpan.Zero)
    {
      await Task.Delay(this.IndexingPause);
    }

    return indexResponse;
  }

  public async Task<TypeSenseDocument> RemoveContent(string id)
  {
    ITypesenseClient client = await GetClient();
    return await client.DeleteDocument<TypeSenseDocument>(this.IndexName, id);
  }

  public async Task<TypeSenseDocument> GetContentByKey(string id)
  {
    ITypesenseClient client = await GetClient();
    return await client.RetrieveDocument<TypeSenseDocument>(this.IndexName, id);
  }

  public async Task<SearchResult<TypeSenseDocument>> Search(SearchQuery query)
  {
    ITypesenseClient client = await GetClient();
    SearchParameters searchParameters;


    if (query.SearchTerm == string.Empty)
    {
      throw new ApplicationException("No search term");
    }
    else
    {
      searchParameters = new(query.SearchTerm)
      {
        HighlightStartTag = "<em>",
        HighlightEndTag = "</em>",
        // IncludeTotalCount = true,
        HighlightFields = BuildList(BuildHighlightFields(query)),
        IncludeFields = BuildList(BuildSelectFields()),
        PerPage = query.PagingSettings.PageSize,
        Page = query.PagingSettings.CurrentPageIndex,
        QueryBy = BuildList(BuildSearchFields()),
        FilterBy = BuildFilter($"{BuildSiteFilter(query)}", $"{BuildRolesFilter(query)}", $"{BuildScopeFilter(query)}", $"{BuildArgsFilter(query)}", $"{BuildPageNumberFilter(query)}"),
        SortBy = "_text_match:desc"
      };
    }

    SearchResult<TypeSenseDocument> response = await client.Search<TypeSenseDocument>(this.IndexName, searchParameters);

    return ReplaceHighlights(response);
  }

  private SearchResult<TypeSenseDocument> ReplaceHighlights(SearchResult<TypeSenseDocument> response)
  {
    // replace document field values with highlighted ones
    foreach (Hit<TypeSenseDocument> objHit in response.Hits)
    {
      // ?? objHit.Source.Score = objHit.VectorDistance;

      foreach (Highlight highlight in objHit.Highlights)
      {
        if (!string.IsNullOrEmpty(highlight.Snippet))
        {
          string strField = highlight.Field;

          switch (strField)
          {
            case "title":
            {

              objHit.Document.Title = highlight.Snippet;

              break;
            }
            case "summary":
            {
              objHit.Document.Summary = highlight.Snippet;
              break;
            }
          }
        }
      }
    }

    return response;
  }

  private static string BuildFilter(params string[] values)
  {
    // filter null values, add spaces in between filter expressions
    return String.Join(" && ", values.Where(value => !String.IsNullOrEmpty(value)));
  }

  private static string BuildList(List<string> values)
  {
    // filter null values, add spaces in between filter expressions
    return String.Join(',', values.Where(value => !String.IsNullOrEmpty(value)));
  }

  private static List<string> BuildSearchFields()
  {
    return
    [
      CamelCase(nameof(TypeSenseDocument.Title)),
      CamelCase(nameof(TypeSenseDocument.Summary)),
      CamelCase(nameof(TypeSenseDocument.Content)),
      CamelCase(nameof(TypeSenseDocument.Keywords)),
      CamelCase(nameof(TypeSenseDocument.Categories)),
      CamelCase(nameof(TypeSenseDocument.TitleVector))//,
      //CamelCase(nameof(TypeSenseDocument.ContentVector)),
      //CamelCase(nameof(TypeSenseDocument.SummaryVector))
    ];
  }

  private static List<string> BuildSuggesterFields()
  {
    return
    [
      CamelCase(nameof(TypeSenseDocument.Title))
    ];
  }

  private static List<string> BuildSelectFields()
  {
    return
    [
      CamelCase(nameof(TypeSenseDocument.SiteId)),
      CamelCase(nameof(TypeSenseDocument.Url)),
      CamelCase(nameof(TypeSenseDocument.Title)),
      CamelCase(nameof(TypeSenseDocument.Summary)),
      CamelCase(nameof(TypeSenseDocument.Scope)),
      CamelCase(nameof(TypeSenseDocument.Type)),
      CamelCase(nameof(TypeSenseDocument.SourceId)),
      CamelCase(nameof(TypeSenseDocument.ContentType)),
      CamelCase(nameof(TypeSenseDocument.PublishedDate)),
      CamelCase(nameof(TypeSenseDocument.Size)),
      CamelCase(nameof(TypeSenseDocument.Keywords)),
      CamelCase(nameof(TypeSenseDocument.Categories)),
      CamelCase(nameof(TypeSenseDocument.Roles))
    ];
  }

  private static List<string> BuildHighlightFields(SearchQuery query)
  {
    return
    [
      CamelCase(nameof(TypeSenseDocument.Title)),
      CamelCase(nameof(TypeSenseDocument.Summary)),
      CamelCase(nameof(TypeSenseDocument.Content))
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

    return $"{CamelCase(nameof(TypeSenseDocument.SiteId))}:={query.Site.Id.ToString()}";
  }

  private static string BuildParentIdFilter(string parentId)
  {
    // https://typesense.org/docs/26.0/api/search.html#filter-parameters

    // Id.ToString is required here, SearchFilter.Create cannot handle Guids
    return $"{CamelCase(nameof(TypeSenseDocument.ParentId))}:={$"{parentId}"}";
  }

  private static string BuildPageNumberFilter(SearchQuery query)
  {
    // We exclude "page 0" from search results, because it is a repeat of the main document (index entry)
    return $"{CamelCase(nameof(TypeSenseDocument.PageNumber))}:!=1";
  }

  private static string BuildRolesFilter(SearchQuery query)
  {
    if (query.Roles?.Any() == true)
    {
      // Id.ToString is required here, SearchFilter.Create cannot handle Guids
      string roles = $"{String.Join(",", query.Roles.Select(role => role.Id.ToString()))}";
      return $"{CamelCase(nameof(TypeSenseDocument.Roles))}:[{roles}] || {CamelCase(nameof(TypeSenseDocument.IsSecure))}:!=true)";
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
      string scopes = $"{String.Join(",", query.IncludedScopes.Select(scope => scope))}";
      result += $"{CamelCase(nameof(TypeSenseDocument.Scope))}:[{scopes}]";
    }

    if (query.ExcludedScopes.Count != 0)
    {
      string scopes = $"{String.Join(",", query.ExcludedScopes.Select(scope => scope))}";
      string result2 = $"{CamelCase(nameof(TypeSenseDocument.Scope))}:!=[{scopes}]";

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

  static public string CamelCase(string fieldName)
  {
    return fieldName.Substring(0, 1).ToLower() + fieldName.Substring(1);
  }
}