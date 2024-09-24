using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Enumeration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Typesense;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;
using System.Net.Http;

namespace Nucleus.Extensions.TypeSense;

#nullable enable

[DisplayName("TypeSense Search Provider")]
public partial class TypeSenseSearchProvider : ISearchProvider
{
  private IHttpClientFactory HttpClientFactory { get; }

  private ISiteManager SiteManager { get; }
  private IListManager ListManager { get; }
  private IRoleManager RoleManager { get; }

  private ILogger<TypeSenseSearchProvider> Logger { get; }


  [System.Text.RegularExpressions.GeneratedRegex("<em>(?<term>[^\\/]*)<\\/em>")]
  private static partial System.Text.RegularExpressions.Regex EXTRACT_HIGHLIGHTED_TERMS_REGEX();

  public TypeSenseSearchProvider(IHttpClientFactory httpClientFactory, ISiteManager siteManager, IListManager listManager, IRoleManager roleManager, ILogger<TypeSenseSearchProvider> logger)
  {
    this.HttpClientFactory = httpClientFactory;
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

    Models.Settings settings = new(query.Site);

    if (String.IsNullOrEmpty(settings.ServerUrl))
    {
      throw new InvalidOperationException($"The TypeSense search server url is not set for site '{query.Site.Name}'.");
    }

    if (String.IsNullOrEmpty(settings.IndexName))
    {
      throw new InvalidOperationException($"The TypeSense search index name is not set for site '{query.Site.Name}'.");
    }


    TypeSenseRequest request = CreateRequest(query.Site, settings);

    //if (query.Boost == null)
    //{
    //  query.Boost = settings.Boost;
    //}

    SearchResult<TypeSenseDocument> response = await request.Search(query);

    IEnumerable<SearchResult> results = await ToSearchResults(query.Site, response);

    return new SearchResults()
    {
      Results = results,
      Answers = [],//await ToSemanticResults(query.Site, response),
      Total = response.Found,
      MaxScore = results.FirstOrDefault()?.Score ?? 0
    };
  }

  private async Task<TypeSenseDocument> GetDocumentByKey(Site site, string key)
  {
    Models.Settings settings = new(site);
    TypeSenseRequest request = CreateRequest(site, settings);
    return await request.GetContentByKey(key);
  }

  private async Task<IEnumerable<SearchResult>> ToSearchResults(Site site, SearchResult<TypeSenseDocument> response)
  {
    List<SearchResult> results = [];

    foreach (Hit<TypeSenseDocument> result in response.Hits)
    {
      results.Add(await ToSearchResult(site, result));
    }

    return results;
  }

  private async Task<SearchResult> ToSearchResult(Site site, Hit<TypeSenseDocument> result)
  {
    // https://threads.typesense.org/2J3b99d
    // TODO: Next version of Typesense is meant to have an improved "displayable" score

    // this does not work very well
    var score =
      ((result.TextMatchInfo?.FieldsMatched ?? 1) * 1.2) + 
      ((result.TextMatchInfo?.TokensMatched ?? 1) * 1.3) 
      + (result.VectorDistance ?? 0) * 0.4;

    //result.TextMatchInfo.TokensMatched

    return new SearchResult()
    {
      Score = score, //double.Parse( result.TextMatchInfo?.Score ?? "0"),

      //MatchedTerms = result.Highlights?
      //  .SelectMany(highlight => highlight.Value)
      //  .SelectMany(highlightValue => ExtractHighlightedTerms(highlightValue))
      //  .Where(value => !String.IsNullOrEmpty(value))
      //  // https://developer.mozilla.org/en-US/docs/Web/URI/Fragment/Text_fragments: Matches are case-insensitive.
      //  .Distinct(StringComparer.OrdinalIgnoreCase)
      //  .ToList(),

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

    Models.Settings settings = new(query.Site);

    if (String.IsNullOrEmpty(settings.ServerUrl))
    {
      throw new InvalidOperationException($"The Elastic search server url is not set for site '{query.Site.Name}'.");
    }

    if (String.IsNullOrEmpty(settings.IndexName))
    {
      throw new InvalidOperationException($"The Elastic search index name is not set for site '{query.Site.Name}'.");
    }

    //if (query.Boost == null)
    //{
    //  query.Boost = settings.Boost;
    //}

    TypeSenseRequest request = CreateRequest(query.Site, settings);

    //todo
    return new SearchResults()
    {
      Results = [],
      Total = 0
    };

    //Response<SuggestResults<TypeSenseDocument>> result = await request.Suggest(query);

    //return new SearchResults()
    //{
    //  Results = await ToSearchResults(query.Site, result),
    //  Total = result.Value.Results.Count
    //};
  }

  private TypeSenseRequest CreateRequest(Site site, Models.Settings settings)
  {
    return new
    (
      this.HttpClientFactory,
      new System.Uri(settings.ServerUrl),
      settings.IndexName,
      Models.Settings.DecryptApiKey(site, settings.EncryptedApiKey),
      TimeSpan.FromSeconds(settings.IndexingPause)
    );
  }

  //private async Task<IEnumerable<SearchResult>> ToSearchResults(Site site, SearchResult<TypeSenseDocument> response)
  //{
  //  List<SearchResult> results = [];

  //  foreach (Hit<TypeSenseDocument> document in response.Hits)
  //  {
  //    results.Add(await ToSearchResult(site, document));
  //  }

  //  return results;
  //}

  //private async Task<SearchResult> ToSearchResult(Site site, Hit<TypeSenseDocument> result)
  //{
  //  return new SearchResult()
  //  {      
  //    Score = double.Parse(result.TextMatchInfo?.Score ?? "0"),

  //    Site = site,
  //    Url = result.Document.Url,
  //    Title = result.Document.Title,
  //    Summary = result.Document.Summary,

  //    Scope = result.Document.Scope,
  //    Type = result.Document.Type,
  //    SourceId = String.IsNullOrEmpty(result.Document.SourceId) ? null : Guid.Parse(result.Document.SourceId),
  //    ContentType = result.Document.ContentType,
  //    PublishedDate = result.Document.PublishedDate,

  //    Size = result.Document.Size,
  //    Keywords = result.Document.Keywords,
  //    Categories = await ToCategories(result.Document.Categories),
  //    Roles = await ToRoles(result.Document.Roles)
  //  };
  //}

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
