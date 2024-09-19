using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;
using Nucleus.Extensions.BingCustomSearch.Models;

namespace Nucleus.Extensions.BingCustomSearch;

[DisplayName("Bing Custom Search Provider")]
public class SearchProvider : ISearchProvider
{
  private ISiteManager SiteManager { get; set; }
  
  private IHttpClientFactory HttpClientFactory { get; set; }

  public SearchProvider(ISiteManager siteManager, IHttpClientFactory httpClientFactory)
  {
    this.SiteManager = SiteManager;
    this.HttpClientFactory = httpClientFactory;
  }

  public ISearchProviderCapabilities GetCapabilities()
  {
    return new DefaultSearchProviderCapabilities()
    {
      CanReportCategories = false,
      CanReportScore = false,
      CanReportSize = false,
      MaximumSuggestions = 0,
      CanReportPublishedDate = false,
      CanReportType = false,
      CanFilterByScope = false,
      MaximumPageSize = 250,
      CanReportMatchedTerms = false
    };
  }

  public async Task<SearchResults> Search(SearchQuery query)
  {
    if (query.Site == null)
    {
      throw new ArgumentException($"The site property is required.", nameof(query.Site));
    }

    Models.Settings settings = new();
    settings.GetSettings(query.Site);

    Models.BingCustomSearchResponse response = await BingApi.GetBingSearchResponse(this.HttpClientFactory, query, settings);
        
    return new()
    {
      Results = ToSearchResults(query.Site, response) ?? new List<SearchResult>(),
      Total = response.WebPages?.TotalEstimatedMatches ?? (long?)response.WebPages?.Value?.Count ?? (long)0
    };
  }

  private IEnumerable<SearchResult> ToSearchResults(Site site, BingCustomSearchResponse response)
  {
    return response.WebPages?.Value?.Select(result => ToSearchResult(site, result)).ToList();
  }

  private SearchResult ToSearchResult(Site site, WebPage result)
  {
    return new SearchResult()
    {
      Site = site,
      Url = result.Url,
      Title = result.Name,
      Summary = result.Snippet,
      ContentType = "text/html"
    };
  }

  public Task<SearchResults> Suggest(SearchQuery query)
  {
    // the Bing Custom search provider does not support search suggestions. The documentation indicates that it works, but 
    // the endpoint always returns "Autosuggest is not yet configured for this instance. Changes may take up to 24 hours to reflect",
    // even though we have enabled at https://www.customsearch.ai/
    return Task.FromResult(new SearchResults()
    {
      Total = 0
    });

    //if (query.Site == null)
    //{
    //  throw new ArgumentException($"The site property is required.", nameof(query.Site));
    //}

    //Models.Settings settings = new();
    //settings.GetSettings(query.Site);

    //Models.BingCustomSuggestions suggestions = await BingApi.GetBingSuggestions(this.HttpClientFactory, query, settings);

    //IEnumerable<SearchResult> results = ToSuggestionResults(query.Site, suggestions)?.Take(query.PagingSettings.PageSize);

    //return new SearchResults()
    //{
    //  Results = results,
    //  Total = (long?)results?.Count() ?? 0
    //};
  }

  private IEnumerable<SearchResult> ToSuggestionResults(Site site, BingCustomSuggestions suggestions)
  {
    return suggestions.SuggestionGroups?.SelectMany(group => group.SearchSuggestions)?.Select(result => ToSuggestionResult(site, result)).ToList();
  }

  private SearchResult ToSuggestionResult(Site site, SearchAction searchAction)
  {
    return new SearchResult()
    {
      Site = site,
      Title = searchAction.DisplayText
    };
  }
}
