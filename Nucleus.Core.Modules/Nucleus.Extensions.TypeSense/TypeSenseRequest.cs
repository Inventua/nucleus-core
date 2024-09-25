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
using DocumentFormat.OpenXml.Bibliography;

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

  internal static readonly char[] TOKEN_SPLIT_CHARS = [' ', ',', ';', ':', '<', '>', '.', '!', '#', '_', '\t', '\n', '\r'];
  internal const int TOKENS_PER_PAGE = 2500;
  internal const int BATCH_SIZE = 10;

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
        new Field(CamelCase(nameof(TypeSenseDocument.Content)), FieldType.String, false, true),        
        new Field(CamelCase(nameof(TypeSenseDocument.Summary)), FieldType.String, false, true),

        new Field(CamelCase(nameof(TypeSenseDocument.Embeddings)), FieldType.FloatArray, false, true)
        {
          Embed = new
          (
            [
              CamelCase(nameof(TypeSenseDocument.Title)),
              CamelCase(nameof(TypeSenseDocument.Summary)),
              CamelCase(nameof(TypeSenseDocument.Content))
            ], 
            new("ts/gte-large") 
          )
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

  public async Task IndexContent(List<TypeSenseDocument> contents)
  {
    // break large content into chunks
    foreach (TypeSenseDocument content in contents.ToList())
    {
      if (!String.IsNullOrEmpty(content.Content))
      {
        // try to fit into the token limit.  

        string[] tokens = content.Content.Split(TOKEN_SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int pages = (int)Math.Floor((decimal)tokens.Length / TOKENS_PER_PAGE) + 1;
        string[] tokenizedContent = content.Content.Split(TOKEN_SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // if the content has more than TOKENS_PER_PAGE tokens, create additional chunks
        if (tokenizedContent.Length > TOKENS_PER_PAGE)
        {
          for (int page = 0; page < pages; page++)
          {
            string vectorContent = string.Join(' ', tokenizedContent.Skip(page * TOKENS_PER_PAGE).Take(TOKENS_PER_PAGE));

            TypeSenseDocument pagedDocument = new();

            pagedDocument.Id += $"{content.Id}_pages_{page + 1}";
            pagedDocument.Title += $" [{page}]";
            pagedDocument.ParentId = content.Id;
            pagedDocument.PageNumber = page + 1;

            CopyMetaData(content, pagedDocument);
            pagedDocument.Content = vectorContent;

            contents.Add(pagedDocument);
          }
        }
      }
    }

    ITypesenseClient client = await GetClient();

    IEnumerable<TypeSenseDocument[]> batchChunks = contents.Chunk(BATCH_SIZE);

    foreach (TypeSenseDocument[] chunk in batchChunks)
    {
      List<ImportResponse> indexResponse = await client.ImportDocuments<TypeSenseDocument>(this.IndexName, chunk, chunk.Length, ImportType.Upsert);

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
    }

    return;
  }

  private static void CopyMetaData(TypeSenseDocument source, TypeSenseDocument target)
  {
    target.SiteId = source.SiteId;
    target.Url = source.Url;

    target.Title = source.Title;

    if (string.IsNullOrEmpty(target.Summary))
    {
      target.Summary = source.Summary;
    }

    target.Size = source.Size;
    target.ContentType = source.ContentType;
    target.Categories = source.Categories;
    target.Keywords = source.Keywords;
    
    target.PublishedDate = source.PublishedDate;
    target.Scope = source.Scope;
    target.SourceId = source.SourceId;
    target.Type = source.Type;

    target.IsSecure = source.IsSecure;
    target.Roles = source.Roles;
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

    if (query.SearchTerm == string.Empty)
    {
      throw new ApplicationException("No search term");
    }
    else
    {
      SearchParameters searchParameters = new(query.SearchTerm)
      {
        HighlightStartTag = "<em>",
        HighlightEndTag = "</em>",
        HighlightFields = BuildList(BuildHighlightFields(query)),
        IncludeFields = BuildList(BuildSelectFields()),
        PerPage = query.PagingSettings.PageSize,
        Page = query.PagingSettings.CurrentPageIndex,
        QueryBy = BuildList(BuildSearchFields()),
        QueryByWeights = BuildFieldWeights(query),
        FilterBy = BuildFilter($"{BuildSiteFilter(query)}", $"{BuildRolesFilter(query)}", $"{BuildScopeFilter(query)}", $"{BuildArgsFilter(query)}", $"{BuildPageNumberFilter(query)}"),
        TextMatchType = "max_score",
        SortBy = "_text_match:desc"
      };

      SearchResult<TypeSenseDocument> response = await client.Search<TypeSenseDocument>(this.IndexName, searchParameters);
   
      return ReplaceHighlights(response);
    }
  }

  public async Task<SearchResult<TypeSenseDocument>> Suggest(SearchQuery query)
  {
    ITypesenseClient client = await GetClient();

    if (query.SearchTerm == string.Empty)
    {
      throw new ApplicationException("No search term");
    }
    else
    {
      SearchParameters searchParameters = new(query.SearchTerm + "*")
      {
        HighlightStartTag = "",
        HighlightEndTag = "", 
        IncludeFields = BuildList(BuildSelectFields()),
        PerPage = query.PagingSettings.PageSize,
        Page = query.PagingSettings.CurrentPageIndex,
        QueryBy = BuildList(BuildSearchFields()),
        QueryByWeights = BuildFieldWeights(query),
        FilterBy = BuildFilter($"{BuildSiteFilter(query)}", $"{BuildRolesFilter(query)}", $"{BuildScopeFilter(query)}", $"{BuildArgsFilter(query)}", $"{BuildPageNumberFilter(query)}"),
        TextMatchType = "max_score",
        SortBy = "_text_match:desc"
      };

      SearchResult<TypeSenseDocument> response = await client.Search<TypeSenseDocument>(this.IndexName, searchParameters);

      return ReplaceHighlights(response);
    }
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
    // order is important: a field earlier in the list of query_by fields is considered more relevant than a document matched
    // on a field later in the list
    return
    [
      CamelCase(nameof(TypeSenseDocument.Title)),
      CamelCase(nameof(TypeSenseDocument.Embeddings)),
      CamelCase(nameof(TypeSenseDocument.Summary)),
      CamelCase(nameof(TypeSenseDocument.Keywords)),
      CamelCase(nameof(TypeSenseDocument.Categories)),
      CamelCase(nameof(TypeSenseDocument.Content))
    ];
  }


  private static string BuildFieldWeights(SearchQuery query)
  {
    // these MUST be in the same order as the fields returned by BuildSearchFields
    return $"{query.Boost.Title},4,{query.Boost.Summary},{query.Boost.Keywords},{query.Boost.Categories},{query.Boost.Content}";
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
      CamelCase(nameof(TypeSenseDocument.Id)),
      CamelCase(nameof(TypeSenseDocument.SiteId)),
      CamelCase(nameof(TypeSenseDocument.Url)),
      CamelCase(nameof(TypeSenseDocument.Title)),
      CamelCase(nameof(TypeSenseDocument.PageNumber)),
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
      return $"({CamelCase(nameof(TypeSenseDocument.Roles))}:[{roles}] || {CamelCase(nameof(TypeSenseDocument.IsSecure))}:!=true)";
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
        result = $"({result} && {result2})";
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