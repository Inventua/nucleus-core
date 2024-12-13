using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Search;
using Typesense;

namespace Nucleus.Extensions.TypeSense;

internal class TypeSenseRequest
{
  // https://github.com/DAXGRID/typesense-dotnet

  System.Net.Http.IHttpClientFactory HttpClientFactory { get; }
  public Uri Uri { get; }
  public string IndexName { get; }

  private string ApiKey { get; }

  private TimeSpan IndexingPause { get; } = TimeSpan.FromSeconds(1);

  private Typesense.ITypesenseClient _client { get; set; }
  private ILogger Logger { get; }

  public string DebugInformation { get; internal set; }

  internal static readonly char[] WORD_BREAKING_CHARS = [' ', ',', ';', ':', '<', '>', '.', '!', '#', '_', '/', '\\', '\t', '\n', '\r'];
  internal const int WORDS_PER_CHUNK = 512;  // the token limit for gte-large is 512
  internal const int IMPORT_BATCH_SIZE = 32;
  internal const string CHUNK_ENTITY_NAME = "chunk";

  public TypeSenseRequest(System.Net.Http.IHttpClientFactory httpClientFactory, Uri uri, string indexName, string apiKey, ILogger logger)
    : this(httpClientFactory, uri, indexName, apiKey, TimeSpan.Zero, logger) { }

  public TypeSenseRequest(System.Net.Http.IHttpClientFactory httpClientFactory, Uri uri, string indexName, string apiKey, TimeSpan indexingPause, ILogger logger)
  {
    this.HttpClientFactory = httpClientFactory;
    this.Uri = uri;
    this.IndexName = indexName;
    this.ApiKey = apiKey;
    this.IndexingPause = indexingPause;
    this.Logger = logger;
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

    IOptions<Typesense.Setup.Config> options = Options.Create(config);

    System.Net.Http.HttpClient httpClient = this.HttpClientFactory.CreateClient(nameof(TypeSenseRequest));
    httpClient.Timeout = TimeSpan.FromSeconds(180);
    
    ITypesenseClient client = new TypesenseClient(options, httpClient);
    
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
        new Field(CamelCase(nameof(TypeSenseDocument.ParentId)), FieldType.String, true, true),
        new Field(CamelCase(nameof(TypeSenseDocument.SiteId)), FieldType.String, true, false),
        new Field(CamelCase(nameof(TypeSenseDocument.Url)), FieldType.String, false, false),
        
        new Field(CamelCase(nameof(TypeSenseDocument.ChunkNumber)), FieldType.Int32, false, true),
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

        new Field(CamelCase(nameof(TypeSenseDocument.Categories)), FieldType.StringArray, true, true),
        new Field(CamelCase(nameof(TypeSenseDocument.Keywords)), FieldType.StringArray, true, true),
        new Field(CamelCase(nameof(TypeSenseDocument.Scope)), FieldType.String, true, true),
        new Field(CamelCase(nameof(TypeSenseDocument.SourceId)), FieldType.String, false, true),

        new Field(CamelCase(nameof(TypeSenseDocument.ContentType)), FieldType.String, false, true),
        new Field(CamelCase(nameof(TypeSenseDocument.PublishedDate)), FieldType.String, false, true),
        new Field(CamelCase(nameof(TypeSenseDocument.Type)), FieldType.String, true, true),
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

  public async Task IndexContent(TypeSenseDocument document)
  {
    List<TypeSenseDocument> actions = [];

    // break large content into chunks    
    if (!String.IsNullOrEmpty(document.Content))
    {
      foreach (TypeSenseDocument action in SplitContent(document))
      {
        actions.Add(action);
      }        
    }
    
    // include original document
    actions.Insert(0, document);

    ITypesenseClient client = await GetClient();

    foreach(TypeSenseDocument[] block in actions.Chunk(IMPORT_BATCH_SIZE))
    {
      await ExecuteBatch(client, document.Id, block);
    }   

    return;
  }

  private async Task ExecuteBatch(ITypesenseClient client, string key, TypeSenseDocument[] batch)
  {
    List<ImportResponse> indexResponse = await client.ImportDocuments<TypeSenseDocument>(this.IndexName, batch, batch.Length, ImportType.Upsert);

    foreach (ImportResponse response in indexResponse)
    {
      if (!response.Success)
      {
        Logger.LogError("IndexContent for [{key}] succeeded with error '{error}'.", key, response.Error);
        throw new InvalidOperationException(response.Error);
      }
    }    

    if (this.IndexingPause > TimeSpan.Zero)
    {
      await Task.Delay(this.IndexingPause);
    }
  }

  private IEnumerable<TypeSenseDocument> SplitContent(TypeSenseDocument contentItem)
  {
    if (!String.IsNullOrEmpty(contentItem.Content))
    {
      string[] tokens = contentItem.Content.Split(WORD_BREAKING_CHARS, StringSplitOptions.RemoveEmptyEntries);
      int pageCount = (int)Math.Floor((decimal)tokens.Length / WORDS_PER_CHUNK) + 1;
      string[] contentWords = contentItem.Content.Split(WORD_BREAKING_CHARS, StringSplitOptions.RemoveEmptyEntries);
      List<string[]> chunks = contentWords.Chunk(WORDS_PER_CHUNK).ToList();

      if (contentWords.Length <= WORDS_PER_CHUNK)
      {
        // document token count is less than the chunk limit, do nothing. The original document index entry is submitted 
        this.Logger.LogInformation("The document content fits the vector token limit, generating a vector for the original document content of type '{type},{id}', url '{url}'.", contentItem.Scope, contentItem.SourceId, contentItem.Url);
      }
      else
      {
        this.Logger.LogInformation("The document content exceeds the vector token limit, generating {pagecount} chunked index entries for content of type '{type},{id}', url '{url}'.", pageCount, contentItem.Scope, contentItem.SourceId, contentItem.Url);

        // set .Content property on the main index entry to null - we are chunking because it exceeds the limit. The chunks array already contains the split/chunked content - if
        // we left .Content set, we would get a Request Entity Too Large error response when submitting the index update.
        contentItem.Content = null;

        // generate document chunks. TypeSense generates vectors on its own, so we don't need to create them here, just make sure the index entries are 
        // split into chunks with Content that is small enough to vectorize.
        for (int page = 0; page < pageCount; page++)
        {
          this.Logger.LogTrace("Generating chunk #{page}.", page);

          IEnumerable<string> words = chunks[page].Where(chunk => chunk.Length > 1 && chunk.Length < 30);
          string vectorContent = string.Join(" ", words);
          int tokenCount = words.Count();

          TypeSenseDocument pagedDocument = new();

          CopyMetaData(contentItem, pagedDocument);

          pagedDocument.ChunkNumber = page + 1;

          pagedDocument.Id = $"{contentItem.Id}_{CHUNK_ENTITY_NAME}_{pagedDocument.ChunkNumber}";

          pagedDocument.IndexingDate = DateTime.UtcNow;

          pagedDocument.ParentId = contentItem.Id;
          pagedDocument.Title += $" [{pagedDocument.ChunkNumber}]";
          pagedDocument.IndexingDate = DateTime.UtcNow;

          // only try to generate vectors if the "tokenized" content is not empty
          if (!string.IsNullOrEmpty(vectorContent))
          {
            pagedDocument.Content = vectorContent;
            
            // submit chunked index entry            
            this.Logger.LogTrace("Returning chunk #{page}.", page);
            yield return pagedDocument;            
          }
        }
      }
    }
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

  public async Task<SearchGroupedResult<TypeSenseDocument>> Search(SearchQuery query)
  {
    ITypesenseClient client = await GetClient();

    if (query.SearchTerm == string.Empty)
    {
      throw new ApplicationException("No search term");
    }
    else
    {
      string groupBy = CamelCase(nameof(TypeSenseDocument.ParentId));

      GroupedSearchParameters searchParameters = new(query.SearchTerm, "", groupBy) //CamelCase(nameof(TypeSenseDocument.ParentId)))
      {
        HighlightStartTag = "<em>",
        HighlightEndTag = "</em>",
        HighlightFields = BuildList(BuildHighlightFields(query)),
        IncludeFields = BuildList(BuildSelectFields()),
        PerPage = query.PagingSettings.PageSize,
        Page = query.PagingSettings.CurrentPageIndex,
        QueryBy = BuildList(BuildSearchFields()),
        QueryByWeights = BuildFieldWeights(query),
        FilterBy = BuildFilter($"{BuildSiteFilter(query)}", $"{BuildRolesFilter(query)}", $"{BuildScopeFilter(query)}", $"{BuildArgsFilter(query)}"),
        TextMatchType = "max_score",
        SortBy = "_text_match:desc",
        GroupMissingValues = false, 
        GroupLimit = 1
      };


      SearchGroupedResult<TypeSenseDocument> response = await client.SearchGrouped<TypeSenseDocument>(this.IndexName, searchParameters);
   
      return ReplaceHighlights(response);
    }
  }

  public async Task<SearchGroupedResult<TypeSenseDocument>> Suggest(SearchQuery query)
  {
    ITypesenseClient client = await GetClient();

    if (query.SearchTerm == string.Empty)
    {
      throw new ApplicationException("No search term");
    }
    else
    {
      string groupBy = CamelCase(nameof(TypeSenseDocument.ParentId));

      GroupedSearchParameters searchParameters = new(query.SearchTerm + "*", "", groupBy)
      {
        HighlightStartTag = "",
        HighlightEndTag = "", 
        IncludeFields = BuildList(BuildSelectFields()),
        PerPage = query.PagingSettings.PageSize,
        Page = query.PagingSettings.CurrentPageIndex,
        QueryBy = BuildList(BuildSearchFields()),
        QueryByWeights = BuildFieldWeights(query),
        FilterBy = BuildFilter($"{BuildSiteFilter(query)}", $"{BuildRolesFilter(query)}", $"{BuildScopeFilter(query)}", $"{BuildArgsFilter(query)}"),
        TextMatchType = "max_score",
        SortBy = "_text_match:desc",
        GroupMissingValues = false,
        GroupLimit = 1
      };

      SearchGroupedResult<TypeSenseDocument> response = await client.SearchGrouped<TypeSenseDocument>(this.IndexName, searchParameters);

      return ReplaceHighlights(response);
    }
  }


  private SearchGroupedResult<TypeSenseDocument> ReplaceHighlights(SearchGroupedResult<TypeSenseDocument> response)
  {
    // replace document field values with highlighted ones
    foreach (GroupedHit<TypeSenseDocument> groupedHit in response.GroupedHits)
    {
      // ?? objHit.Source.Score = objHit.VectorDistance;

      foreach (Hit<TypeSenseDocument> hit in groupedHit.Hits)
      {
        foreach (Highlight highlight in hit.Highlights)
        {
          if (!string.IsNullOrEmpty(highlight.Snippet))
          {
            string strField = highlight.Field;

            switch (strField)
            {
              case "title":
              {

                hit.Document.Title = highlight.Snippet;

                break;
              }
              case "summary":
              {
                hit.Document.Summary = highlight.Snippet;
                break;
              }
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
      CamelCase(nameof(TypeSenseDocument.ParentId)),
      CamelCase(nameof(TypeSenseDocument.SiteId)),
      CamelCase(nameof(TypeSenseDocument.Url)),
      CamelCase(nameof(TypeSenseDocument.Title)),
      CamelCase(nameof(TypeSenseDocument.ChunkNumber)),
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

  //private static string BuildPageNumberFilter(SearchQuery query)
  //{
  //  // We exclude "page 0" from search results, because it is a repeat of the main document (index entry)
  //  return $"{CamelCase(nameof(TypeSenseDocument.PageNumber))}:!=1";
  //}

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