using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Enumeration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.AzureSearch;

#nullable enable

[DisplayName("Azure Search Provider")]
public partial class AzureSearchProvider : ISearchProvider
{
  private ISiteManager SiteManager { get; }
  private IListManager ListManager { get; }
  private IRoleManager RoleManager { get; }

  private ILogger<AzureSearchProvider> Logger { get; }


  [System.Text.RegularExpressions.GeneratedRegex("<em>(?<term>[^\\/]*)<\\/em>")]
  private static partial System.Text.RegularExpressions.Regex EXTRACT_HIGHLIGHTED_TERMS_REGEX();

  public AzureSearchProvider(ISiteManager siteManager, IListManager listManager, IRoleManager roleManager, ILogger<AzureSearchProvider> logger)
  {
    this.SiteManager = siteManager;
    this.ListManager = listManager;
    this.RoleManager = roleManager;
    this.Logger = logger;
  }

  public async Task<SearchResults> Search(SearchQuery query)
  {
    if (query.Site == null)
    {
      throw new InvalidOperationException($"The {nameof(query.Site)} property is required.");
    }

    AzureSearchSettings settings = new(query.Site);

    if (String.IsNullOrEmpty(settings.AzureSearchServiceEndpoint))
    {
      throw new InvalidOperationException($"The Azure search server url is not set for site '{query.Site.Name}'.");
    }

    if (String.IsNullOrEmpty(settings.IndexName))
    {
      throw new InvalidOperationException($"The Azure search index name is not set for site '{query.Site.Name}'.");
    }

    AzureSearchRequest request = CreateRequest(query.Site, settings);
    
    Response<SearchResults<AzureSearchDocument>> response = await request.Search(query);

    return new SearchResults()
    {
      Results = await ToSearchResults(query.Site, response),
      Answers = await ToSemanticResults(query.Site, response),
      Total = response.Value?.TotalCount ?? 0
    };
  }

  private async Task<AzureSearchDocument> GetDocumentByKey(Site site, string key)
  {
    AzureSearchSettings settings = new(site);
    AzureSearchRequest request = CreateRequest(site, settings);
    return await request.GetContentByKey(key);
  }

  private static SearchResult<AzureSearchDocument> ReplaceHighlights(SearchResult<AzureSearchDocument> searchResult)
  {
    if (searchResult.Highlights != null)
    {
      // replace document field values with highlighted ones
      foreach (KeyValuePair<string, IList<string>> highlight in searchResult.Highlights)
      {
        string fieldName = highlight.Key;

        foreach (string higlightItem in highlight.Value)
        {
          switch (fieldName)
          {
            case string name when name.Equals(nameof(AzureSearchDocument.Title), StringComparison.OrdinalIgnoreCase):
            {
              searchResult.Document.Title = higlightItem;
              break;
            }
            case string name when name.Equals(nameof(AzureSearchDocument.Summary), StringComparison.OrdinalIgnoreCase):
            {
              searchResult.Document.Summary = higlightItem;
              break;
            }
          }
        }
      }
    }

    if (searchResult.SemanticSearch != null && searchResult.SemanticSearch?.Captions?.Any() == true)
    {
      searchResult.Document.Summary = String.Join(" ", searchResult.SemanticSearch.Captions.Select(caption => caption.Highlights));
    }

    return searchResult;
  }

  private async Task<IEnumerable<SearchResult>> ToSearchResults(Site site, Response<SearchResults<AzureSearchDocument>> response)
  {
    List<SearchResult> results = [];
        
    foreach (SearchResult<AzureSearchDocument> document in response.Value.GetResultsAsync().ToBlockingEnumerable())
    {
      results.Add(await ToSearchResult(site, ReplaceHighlights(document)));
    }

    return results;
  }

  private async Task<IEnumerable<SemanticResult>> ToSemanticResults(Site site, Response<SearchResults<AzureSearchDocument>> response)
  {
    List<SemanticResult> results = [];

    if (response.Value.SemanticSearch?.Answers?.Any() == true)
    {
      foreach (QueryAnswerResult answer in await SuppressDuplicates(site, response.Value.SemanticSearch?.Answers))
      {
        results.Add(await ToSemanticResult(site, answer));
      }
    }
    return results;
  }

  /// <summary>
  /// Search for cases where a "duplicate" answer has been returned - that is, a parent document AND a chunk index entry were 
  /// both in the returned answers, and remove the parent document, because the chunk entry is "more specific".
  /// </summary>
  /// <param name="site"></param>
  /// <param name="answers"></param>
  /// <returns></returns>
  private async Task<List<QueryAnswerResult>> SuppressDuplicates(Site site, IEnumerable<QueryAnswerResult>? answers)
  {
    List<QueryAnswerResult> results = [];

    if (answers == null) return results;

    foreach (QueryAnswerResult answer in answers)
    {
      AzureSearchDocument relatedDocument = await GetDocumentByKey(site, answer.Key);

      foreach (QueryAnswerResult duplicate in results.Where(result => result.Key == relatedDocument.ParentId).ToList())
      {
        results.Remove(duplicate);
      }

      results.Add(answer);
    }

    return results;
  }

  private async Task<SemanticResult> ToSemanticResult(Site site, QueryAnswerResult result)
  {
    // (QueryAnswerResult)result.Key contains the Id for the document which yielded the answer
    AzureSearchDocument relatedDocument = await GetDocumentByKey(site, result.Key);

    string prefix="";

    if (result.Highlights.Length > 0 && !Char.IsAsciiLetterUpper(result.Highlights.First()))
    {
      prefix = " ...";
    }

    return new SemanticResult()
    {
      Score = result.Score,
      Answer = prefix + result.Highlights,

      MatchedTerms = ExtractHighlightedTerms(result.Highlights)?
        .Where(value => !String.IsNullOrEmpty(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList(),

      Site = site,
      Url = relatedDocument?.Url,
      Title = relatedDocument?.Title,
      Summary = relatedDocument?.Summary,
      
      Scope = relatedDocument?.Scope,
      Type = relatedDocument?.Type,
      SourceId = String.IsNullOrEmpty(relatedDocument?.SourceId) ? null : Guid.Parse(relatedDocument.SourceId),
      ContentType = relatedDocument?.ContentType,
      PublishedDate = relatedDocument?.PublishedDate,

      Size = relatedDocument?.Size,
      Keywords = relatedDocument?.Keywords,
      Categories = await ToCategories(relatedDocument?.Categories),
      Roles = await ToRoles(relatedDocument?.Roles)
    };
  }


  private async Task<SearchResult> ToSearchResult(Site site, SearchResult<AzureSearchDocument> result)
  {
    return new SearchResult()
    {
      Score = result.SemanticSearch?.RerankerScore ?? result.Score,

      MatchedTerms = result.Highlights?
        .SelectMany(highlight => highlight.Value)
        .SelectMany(highlightValue => ExtractHighlightedTerms(highlightValue))
        .Where(value => !String.IsNullOrEmpty(value))
        // https://developer.mozilla.org/en-US/docs/Web/URI/Fragment/Text_fragments: Matches are case-insensitive.
        .Distinct(StringComparer.OrdinalIgnoreCase)  
        .ToList(),

      Site = site,
      Url = result.Document.Url,
      Title = result.Document.Title,
      Summary = result.Document.Summary,

      Scope = result.Document.Scope,
      Type = result.Document.Type,
      SourceId = String.IsNullOrEmpty(result.Document.SourceId) ? null : Guid.Parse(result.Document.SourceId),
      ContentType = result.Document.ContentType,
      PublishedDate = result.Document.PublishedDate,

      IndexedDate = result.Document.IndexingDate,

      Size = result.Document.Size,
      Keywords = result.Document.Keywords,
      Categories = await ToCategories(result.Document.Categories),
      Roles = await ToRoles(result.Document.Roles)
    };
  }

  private static List<string> ExtractHighlightedTerms(string highlightedValue)
  {
    List<string> results = [];

    System.Text.RegularExpressions.MatchCollection matches = EXTRACT_HIGHLIGHTED_TERMS_REGEX().Matches(highlightedValue);
    foreach (System.Text.RegularExpressions.Match match in matches.Cast<Match>())
    {
      if (match.Success)
      {
        {
          results.Add(match.Groups["term"].Value);
        }
      }
    }
    
    return results;
  }

  public async Task<SearchResults> Suggest(SearchQuery query)
  {
    if (query.Site == null)
    {
      throw new InvalidOperationException($"The {nameof(query.Site)} property is required.");
    }

    AzureSearchSettings settings = new(query.Site);

    if (String.IsNullOrEmpty(settings.AzureSearchServiceEndpoint))
    {
      throw new InvalidOperationException($"The Elastic search server url is not set for site '{query.Site.Name}'.");
    }

    if (String.IsNullOrEmpty(settings.IndexName))
    {
      throw new InvalidOperationException($"The Elastic search index name is not set for site '{query.Site.Name}'.");
    }

    AzureSearchRequest request = CreateRequest(query.Site, settings);

    Response<SuggestResults<AzureSearchDocument>> result = await request.Suggest(query);

    return new SearchResults()
    {
      Results = await ToSearchResults(query.Site, result),
      Total = result.Value.Results.Count
    };
  }

  private AzureSearchRequest CreateRequest(Site site, AzureSearchSettings settings)
  {
    return new
    (
      site,
      settings,
      this.Logger
    );
  }

  private async Task<IEnumerable<SearchResult>> ToSearchResults(Site site, Response<SuggestResults<AzureSearchDocument>> response)
  {
    List<SearchResult> results = [];

    foreach (SearchSuggestion<AzureSearchDocument> document in response.Value.Results)
    {
      results.Add(await ToSearchResult(site, document));
    }

    return results;
  }

  private async Task<SearchResult> ToSearchResult(Site site, SearchSuggestion<AzureSearchDocument> result)
  {
    return new SearchResult()
    {
      //Score = document.Score,  // Azure Search doesn't return a score for suggestions
      Site = site,
      Url = result.Document.Url,
      Title = result.Document.Title,
      Summary = result.Document.Summary,

      Scope = result.Document.Scope,
      Type = result.Document.Type,
      SourceId = String.IsNullOrEmpty(result.Document.SourceId) ? null : Guid.Parse(result.Document.SourceId),
      ContentType = result.Document.ContentType,
      PublishedDate = result.Document.PublishedDate,

      Size = result.Document.Size,
      Keywords = result.Document.Keywords,
      Categories = await ToCategories(result.Document.Categories),
      Roles = await ToRoles(result.Document.Roles)
    };
  }

  private async Task<IEnumerable<ListItem>> ToCategories(IEnumerable<string>? idList)
  {
    if (idList == null) return [];

    List<ListItem> results = [];

    foreach (string id in idList)
    {
      results.Add(await this.ListManager.GetListItem(Guid.Parse(id)));
    }

    return results.Where(result => result != null);
  }

  private async Task<IEnumerable<Role>> ToRoles(IEnumerable<string>? idList)
  {
    if (idList == null) return [];

    List<Role> results = [];

    foreach (string id in idList)
    {
      results.Add(await this.RoleManager.Get(Guid.Parse(id)));
    }

    return results.Where(result => result != null);
  }

}
