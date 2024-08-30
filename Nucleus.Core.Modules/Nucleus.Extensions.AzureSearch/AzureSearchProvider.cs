using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Enumeration;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents.Models;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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


    AzureSearchRequest request = CreateRequest(query.Site, settings);

    if (query.Boost == null)
    {
      query.Boost = settings.Boost;
    }

    Response<SearchResults<AzureSearchDocument>> response = await request.Search(query);

    return new SearchResults()
    {
      Results = await ToSearchResults(query.Site, response),
      Answers = await ToSemanticResults(query.Site, response),
      Total = response.Value.TotalCount.HasValue ? response.Value.TotalCount.Value : 0
    };
  }

  private async Task<AzureSearchDocument> GetDocumentByKey(Site site, string key)
  {
    ConfigSettings settings = new(site);
    AzureSearchRequest request = CreateRequest(site, settings);
    return await request.GetContentByKey(key);
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

    if (searchResult.SemanticSearch != null && searchResult.SemanticSearch?.Captions?.Any() == true)
    {
      searchResult.Document.Summary = String.Join(" ", searchResult.SemanticSearch.Captions.Select(caption => caption.Highlights));
    }

    return searchResult;
  }

  private async Task<IEnumerable<SearchResult>> ToSearchResults(Site site, Response<SearchResults<AzureSearchDocument>> response)
  {
    List<SearchResult> results = new();
        
    foreach (SearchResult<AzureSearchDocument> document in response.Value.GetResultsAsync().ToBlockingEnumerable())
    {
      results.Add(await ToSearchResult(site, ReplaceHighlights(document)));
    }

    return results;
  }

  private async Task<IEnumerable<SemanticResult>> ToSemanticResults(Site site, Response<SearchResults<AzureSearchDocument>> response)
  {
    List<SemanticResult> results = new();

    if (response.Value.SemanticSearch?.Answers?.Any() == true)
    {
      foreach (QueryAnswerResult answer in response.Value.SemanticSearch?.Answers)
      {
        results.Add(await ToSemanticResult(site, answer));
      }
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
        .Distinct()
        .ToList(),

      Site = site,
      Url = relatedDocument?.Url,
      Title = relatedDocument?.Title,
      Summary = relatedDocument.Summary,
      
      Scope = relatedDocument?.Scope,
      Type = relatedDocument?.Type,
      SourceId = String.IsNullOrEmpty(relatedDocument?.SourceId) ? null : Guid.Parse(relatedDocument?.SourceId),
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
        .Distinct()
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

    AzureSearchRequest request = CreateRequest(query.Site, settings);

    Response<SuggestResults<AzureSearchDocument>> result = await request.Suggest(query);

    return new SearchResults()
    {
      Results = await ToSearchResults(result),
      Total = result.Value.Results.Count
    };
  }

  private AzureSearchRequest CreateRequest(Site site, ConfigSettings settings)
  {
    return new
    (
      new System.Uri(settings.ServerUrl),
      ConfigSettings.DecryptApiKey(site, settings.EncryptedApiKey),
      settings.IndexName,
      settings.IndexerName,
      settings.SemanticConfigurationName,
      settings.VectorizationEnabled,
      settings.AzureOpenAIEndpoint,
      ConfigSettings.DecryptApiKey(site, settings.EncryptedAzureOpenAIApiKey),
      settings.AzureOpenAIDeploymentName
    );
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
