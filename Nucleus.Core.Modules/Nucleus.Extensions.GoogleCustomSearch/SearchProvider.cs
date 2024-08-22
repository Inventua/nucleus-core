using Google.Apis.CustomSearchAPI.v1;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Extensions.GoogleCustomSearch;

[DisplayName("Google Custom Search Provider")]
public class SearchProvider : ISearchProvider
{
  private ISiteManager SiteManager { get; }

  private static CustomSearchAPIService _service;

  public SearchProvider(ISiteManager siteManager)
  {
    this.SiteManager = siteManager;
  }

  public async Task<SearchResults> Search(SearchQuery query)
  {
    if (query.Site == null)
    {
      throw new ArgumentException($"The site property is required.", nameof(query.Site));
    }

    Models.Settings settings = new();
    settings.GetSettings(query.Site);

    if (String.IsNullOrEmpty(settings.GetApiKey(query.Site)))
    {
      throw new InvalidOperationException("Google custom search Api Key is not set, use the Settings/Google Custom Search settings control panel to configure your settings.");
    }
    if (String.IsNullOrEmpty(settings.SearchEngineId))
    {
      throw new InvalidOperationException("Google custom search Search Engine Id is not set, use the Settings/Google Custom Search settings control panel to configure your settings.");
    }

    if (query.PagingSettings.PageSize > 10 )
    {
      query.PagingSettings.PageSize = 10;
    }
        
    Google.Apis.CustomSearchAPI.v1.CseResource.ListRequest request = this.GetService().Cse.List();

    request.Key = settings.GetApiKey(query.Site);

    request.Q = query.SearchTerm;
    request.Cx = settings.SearchEngineId; 
    request.ExactTerms = query.StrictSearchTerms ? query.SearchTerm : null;
    request.Start = query.PagingSettings.FirstRowIndex+1; 
    request.Num = query.PagingSettings.PageSize;
    
    //request.Safe = CseResource.ListRequest.SafeEnum.Active;  TODO!

    Google.Apis.CustomSearchAPI.v1.Data.Search response = await request.ExecuteAsync();

    if (!long.TryParse(response.SearchInformation.TotalResults, out long totalResultsCount))
    {
      totalResultsCount = response.Items.LongCount();
    }

    return new SearchResults()
    {
      Results = await ToSearchResults(query.Site, response.Items),
      Total = totalResultsCount
    };
  }

  private CustomSearchAPIService GetService()
  {
    if (_service == null)
    {
      _service = new();
    }

    return _service;
  }

  private async Task<IEnumerable<SearchResult>> ToSearchResults(Site site, IList<Google.Apis.CustomSearchAPI.v1.Data.Result> documents)
  {
    List<SearchResult> results = new();

    if (documents != null)
    {
      foreach (Google.Apis.CustomSearchAPI.v1.Data.Result document in documents)
      {
        results.Add(await ToSearchResult(site, document));
      }
    }

    return results;
  }

  private async Task<SearchResult> ToSearchResult(Site site, Google.Apis.CustomSearchAPI.v1.Data.Result document)
  {
    return new SearchResult()
    {
      Site = await this.SiteManager.Get(site.Id),
      Url = document.Link,
      Title = document.Title,
      Summary = document.Snippet,
      ContentType = document.Mime
    };
  }

  public Task<SearchResults> Suggest(SearchQuery query)
  {
    // the Google Custom search provider does not support search suggestions
    return Task.FromResult(new SearchResults()
    {
      Total = 0
    });
  }
}
