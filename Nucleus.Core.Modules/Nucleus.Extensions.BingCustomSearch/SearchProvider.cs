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

//using Microsoft.Azure.CognitiveServices.Search.CustomSearch.Models;
//using Microsoft.Bing.CustomSearch;
//using Microsoft.Bing.CustomSearch.Models;
//using Microsoft.Azure.CognitiveServices.Search.CustomSearch;


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
      MaximumSuggestions = 8,
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

    Models.BingCustomSearchResponse response = await GetBingSearchResponse(query, settings);
        
    return new()
    {
      Results = ToSearchResults(query.Site, response) ?? new List<SearchResult>(),
      Total = response.WebPages.TotalEstimatedMatches ?? (long?)response.WebPages?.Value?.Count ?? (long)0
    };
  }

  
  private async Task<BingCustomSearchResponse> GetBingSearchResponse(SearchQuery query, Models.Settings settings)
  {
    HttpClient httpClient = this.HttpClientFactory.CreateClient();

    string url = $"{Settings.SEARCH_BASE_URI}?q={query.SearchTerm}&customconfig={settings.ConfigurationId}&count={query.PagingSettings.PageSize}&offset={query.PagingSettings.FirstRowIndex}&safeSearch={settings.SafeSearch}&textDecorations=true&textFormat=HTML";

    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
    requestMessage.Headers.Add("Ocp-Apim-Subscription-Key", settings.GetApiKey(query.Site));

    HttpResponseMessage response = await httpClient.SendAsync(requestMessage);

    response.EnsureSuccessStatusCode();

    BingCustomSearchResponse searchResponse = await System.Text.Json.JsonSerializer.DeserializeAsync<BingCustomSearchResponse>(await response.Content.ReadAsStreamAsync());

    return searchResponse;   
  }

  private IEnumerable<SearchResult> ToSearchResults(Site site, BingCustomSearchResponse response)
  {
    return response.WebPages?.Value?.Select(result => ToSearchResult(site, result)).ToList();

    //List<SearchResult> results = new();


    //foreach (WebPage result in response.WebPages.Value)
    //{
    //  results.Add(await ToSearchResult(site, result));
    //}


    //return results;
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


  public async Task<SearchResults> Suggest(SearchQuery query)
  {
    if (query.Site == null)
    {
      throw new ArgumentException($"The site property is required.", nameof(query.Site));
    }

    Models.Settings settings = new();
    settings.GetSettings(query.Site);

    Models.BingCustomSuggestions suggestions = await GetBingSuggestions(query, settings);

    return new SearchResults()
    {
      Results = ToSuggestionResults(query.Site, suggestions),
      Total = (long?)suggestions.SuggestionGroups?.SearchSuggestions?.Count ?? (long)0
    };
  }

  private async Task<BingCustomSuggestions> GetBingSuggestions(SearchQuery query, Models.Settings settings)
  {
    HttpClient httpClient = this.HttpClientFactory.CreateClient();

    string url = $"{Settings.SUGGEST_BASE_URI}?q={query.SearchTerm}&customconfig={settings.ConfigurationId}&count={query.PagingSettings.PageSize}&offset={query.PagingSettings.FirstRowIndex}&safeSearch={settings.SafeSearch}&textDecorations=true&textFormat=HTML";

    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
    requestMessage.Headers.Add("Ocp-Apim-Subscription-Key", settings.GetApiKey(query.Site));

    HttpResponseMessage response = await httpClient.SendAsync(requestMessage);

    response.EnsureSuccessStatusCode();

    BingCustomSuggestions suggestions = await System.Text.Json.JsonSerializer.DeserializeAsync<BingCustomSuggestions>(await response.Content.ReadAsStreamAsync());

    return suggestions;
  }


  private IEnumerable<SearchResult> ToSuggestionResults(Site site, BingCustomSuggestions suggestions)
  {
    return suggestions.SuggestionGroups?.SearchSuggestions?.Select(result => ToSuggestionResult(site, result)).ToList();
  }

  private SearchResult ToSuggestionResult(Site site, SearchAction searchAction)
  {
    return new SearchResult()
    {
      Site = site,
      Title = searchAction.DisplayText,
      ContentType = searchAction.SearchKind
    };
  }
}
