using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Enumeration;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents.Models;
using DocumentFormat.OpenXml.Drawing;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.AzureSearch;

[DisplayName("Azure Search Provider")]
public class AzureSearchProvider : ISearchProvider
{
  private ISiteManager SiteManager { get; }
  private IListManager ListManager { get; }
  private IRoleManager RoleManager { get; }


  public AzureSearchProvider(ISiteManager siteManager, IListManager listManager, IRoleManager roleManager)
  {
    this.SiteManager = siteManager;
    this.ListManager = listManager;
    this.RoleManager = roleManager;
  }


  public async Task<SearchResults> Search(SearchQuery query)
  {
    if (query.Site == null)
    {
      throw new ArgumentException($"The site property is required.", nameof(query.Site));
    }

    ConfigSettings settings = new(query.Site);

    if (String.IsNullOrEmpty(settings.ServerUrl))
    {
      throw new InvalidOperationException($"The Azure search server url is not set for site '{query.Site.Name}'.");
    }

    if (String.IsNullOrEmpty(settings.IndexName))
    {
      throw new InvalidOperationException($"The Azure search index name is not set for site '{query.Site.Name}'.");
    }


    AzureSearchRequest request = new(new System.Uri(settings.ServerUrl), ConfigSettings.DecryptApiKey(query.Site, settings.EncryptedApiKey), settings.IndexName, settings.IndexerName);

    if (query.Boost == null)
    {
      query.Boost = settings.Boost;
    }

    Response<SearchResults<AzureSearchDocument>> response = await request.Search(query);

    return new SearchResults()
    {
      Results = await ToSearchResults(response),
      Total = response.Value.TotalCount.HasValue ? response.Value.TotalCount.Value : 0
    };
  }

  private SearchResult<AzureSearchDocument> ReplaceHighlights(SearchResult<AzureSearchDocument> searchResult)
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

    return searchResult;
  }

  private async Task<IEnumerable<SearchResult>> ToSearchResults(Response<SearchResults<AzureSearchDocument>> response)
  {
    List<SearchResult> results = new();
    //response.Value.SemanticSearch.

    foreach (SearchResult<AzureSearchDocument> document in response.Value.GetResultsAsync().ToBlockingEnumerable())
    {
      results.Add(await ToSearchResult(ReplaceHighlights(document)));
    }

    return results;
  }

  private async Task<SearchResult> ToSearchResult(SearchResult<AzureSearchDocument> result)
  {
    return new SearchResult()
    {
      Score = result.Score,
      MatchedTerms = result.Highlights?
        .SelectMany(highlight => highlight.Value)
        .SelectMany(highlightValue => ExtractHighlightedTerms(highlightValue))
        .Where(value => !String.IsNullOrEmpty(value))
        .Distinct()
        .ToList(),

      Site = await this.SiteManager.Get(Guid.Parse(result.Document.SiteId)),
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

  private List<string> ExtractHighlightedTerms(string highlightedValue)
  {
    List<string> results = new();

    System.Text.RegularExpressions.MatchCollection matches = System.Text.RegularExpressions.Regex.Matches(highlightedValue, "<em>(?<term>[^\\/]*)<\\/em>");
    foreach (System.Text.RegularExpressions.Match match in matches)
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
      throw new ArgumentException($"The site property is required.", nameof(query.Site));
    }

    ConfigSettings settings = new(query.Site);

    if (String.IsNullOrEmpty(settings.ServerUrl))
    {
      throw new InvalidOperationException($"The Elastic search server url is not set for site '{query.Site.Name}'.");
    }

    if (String.IsNullOrEmpty(settings.IndexName))
    {
      throw new InvalidOperationException($"The Elastic search index name is not set for site '{query.Site.Name}'.");
    }

    if (query.Boost == null)
    {
      query.Boost = settings.Boost;
    }

    AzureSearchRequest request = new(new System.Uri(settings.ServerUrl), ConfigSettings.DecryptApiKey(query.Site, settings.EncryptedApiKey), settings.IndexName, settings.IndexerName);

    Response<SuggestResults<AzureSearchDocument>> result = await request.Suggest(query);

    return new SearchResults()
    {
      Results = await ToSearchResults(result),
      Total = result.Value.Results.Count
    };
  }

  private async Task<IEnumerable<SearchResult>> ToSearchResults(Response<SuggestResults<AzureSearchDocument>> response)
  {
    List<SearchResult> results = new();

    foreach (SearchSuggestion<AzureSearchDocument> document in response.Value.Results)
    {
      results.Add(await ToSearchResult(document));
    }

    return results;
  }

  private async Task<SearchResult> ToSearchResult(SearchSuggestion<AzureSearchDocument> result)
  {
    return new SearchResult()
    {
      //Score = document.Score,  // Azure Search doesn't return a score for suggestions

      Site = await this.SiteManager.Get(Guid.Parse(result.Document.SiteId)),
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

  private async Task<IEnumerable<ListItem>> ToCategories(IEnumerable<string> idList)
  {
    if (idList == null) return default;

    List<ListItem> results = new();

    foreach (string id in idList)
    {
      results.Add(await this.ListManager.GetListItem(Guid.Parse(id)));
    }

    return results.Where(result => result != null);
  }

  private async Task<IEnumerable<Role>> ToRoles(IEnumerable<string> idList)
  {
    if (idList == null) return default;

    List<Role> results = new();

    foreach (string id in idList)
    {
      results.Add(await this.RoleManager.Get(Guid.Parse(id)));
    }

    return results.Where(result => result != null);
  }
}
