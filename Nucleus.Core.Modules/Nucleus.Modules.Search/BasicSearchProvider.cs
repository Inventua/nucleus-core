using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Search;
using Nucleus.Extensions;

namespace Nucleus.Modules.Search;

[DisplayName("Basic Search Provider")]
public class BasicSearchProvider : ISearchProvider
{
  private IPageManager PageManager { get; }
  private IFileSystemManager FileSystemManager { get; }

  public BasicSearchProvider(IPageManager pageManager, IFileSystemManager fileSystemManager)
  {
    this.PageManager = pageManager;
    this.FileSystemManager = fileSystemManager;
  }
    
  public async Task<SearchResults> Search(SearchQuery query)
  {
    if (query.Site == null) throw new InvalidOperationException($"The query.Site property is required.");
    
    // we have to query the database twice instead of doing a single search operation, so we have to ignore paging settings for the 
    // initial queries, and then do our own Skip/Take after sorting the results.
    Abstractions.Models.Paging.PagedResult<Page> pages =
      IsScopeIncluded(query, Page.URN) 
        ? await this.PageManager.Search(query.Site, query.SearchTerm, query.Roles, new() { PageSize = Int32.MaxValue }) 
        : null;

    Abstractions.Models.Paging.PagedResult<File> files = 
      IsScopeIncluded(query, Folder.URN) || IsScopeIncluded(query, File.URN) 
        ? await this.FileSystemManager.Search(query.Site, query.SearchTerm, query.Roles, new() { PageSize = Int32.MaxValue }) 
        : null;
    
    List<SearchResult> results = ToSearchResults(query.Site, pages, files)
      .OrderBy(result => result.Title)
      .ToList();

    return new()
    {
      Results = results
                  .Skip(query.PagingSettings.FirstRowIndex)
                  .Take(query.PagingSettings.PageSize),
      Total = results.Count
    };
  }

  public Task<SearchResults> Suggest(SearchQuery query)
  {
    // the basic search provider does not support search suggestions
    return Task.FromResult(new SearchResults()
    {
      Total = 0
    });
  }

  private static Boolean IsScopeIncluded(SearchQuery query, string scope)
  {
    Boolean result = false;

    if (!query.IncludedScopes.Any() || query.IncludedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
    {
      result = true;
    }

    if (query.ExcludedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase)) 
    {
      result = false;
    }
    
    return result;
  }

  private List<SearchResult> ToSearchResults(Site site, Abstractions.Models.Paging.PagedResult<Page> pages, Abstractions.Models.Paging.PagedResult<File> files)
  {
    List<SearchResult> results = [];

    if (pages != null) results.AddRange(pages.Items.Select(page => ToSearchResult(site, page)));
    if (files != null) results.AddRange(files.Items.Select(file => ToSearchResult(site, file)));

    return results;
  }

  private static SearchResult ToSearchResult(Site site, Page page)
  {
    return new SearchResult()
    {
      Site = site,
      Url = page.DefaultPageRoute().Path,
      Title = page.Title,
      Summary = page.Description,

      Scope = Page.URN,
      SourceId = page.Id,
      ContentType = "text/html",
      PublishedDate = page.DateChanged ?? page.DateAdded,
      Type = "Page",

      Keywords = page.Keywords?.Split(',')
    };
  }

  private static SearchResult ToSearchResult(Site site, File file)
  {
    return new SearchResult()
    {
      Site = site,
      Url = $"/files/{file.EncodeFileId()}",
      Title = System.IO.Path.GetFileName(file.Path),

      Scope = File.URN,
      SourceId = file.Id,
      ContentType = "text/html",
      PublishedDate = file.DateChanged ?? file.DateAdded,
      Type = "File",
    };
  }
}
