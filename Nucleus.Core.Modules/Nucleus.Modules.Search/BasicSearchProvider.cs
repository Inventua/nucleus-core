using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Search;
using Nucleus.Extensions;

namespace Nucleus.Modules.Search
{
  [DisplayName("Basic Page Search Provider")]
	public class BasicSearchProvider : ISearchProvider
	{
		private ISiteManager SiteManager { get; }
		private IPageManager PageManager { get; }
    private IFileSystemManager FileSystemManager { get; }

    public BasicSearchProvider(ISiteManager siteManager, IRoleManager roleManager, IPageManager pageManager, IFileSystemManager fileSystemManager)
    {
			this.SiteManager = siteManager;
			this.PageManager = pageManager;
      this.FileSystemManager = fileSystemManager; 
		}

		public async Task<SearchResults> Search(SearchQuery query)
		{
			if (query.Site == null)
			{
				throw new ArgumentException($"The site property is required.", nameof(query.Site));
			}

      Abstractions.Models.Paging.PagedResult<Page> pages = null;
      Abstractions.Models.Paging.PagedResult<File> files = null;

      if (query.IncludedScopes.Contains(Nucleus.Abstractions.Models.Page.URN))
      {
        pages = await this.PageManager.Search(query.Site, query.SearchTerm, query.Roles, query.PagingSettings);
      }

      if (query.IncludedScopes.Contains(Nucleus.Abstractions.Models.FileSystem.Folder.URN) || query.IncludedScopes.Contains(Nucleus.Abstractions.Models.FileSystem.File.URN))
      {
        files = await this.FileSystemManager.Search(query.Site, query.SearchTerm, query.Roles, query.PagingSettings);
      }

			return new SearchResults()
			{
				Results = ToSearchResults(query.Site, pages, files),
				Total = pages?.TotalCount ?? 0 + files?.TotalCount ?? 0
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

		private IEnumerable<SearchResult> ToSearchResults(Abstractions.Models.Site site, Abstractions.Models.Paging.PagedResult<Page> pages, Abstractions.Models.Paging.PagedResult<File> files)
		{
			List<SearchResult> results = new();

      if (pages != null)
      {
        foreach (Page page in pages.Items)
        {
          results.Add(ToSearchResult(site, page));
        }
      }

      if (files != null)
      {
        foreach (File file in files.Items)
        {
          results.Add(ToSearchResult(site, file));
        }
      }

      return results;
		}

		private SearchResult ToSearchResult(Abstractions.Models.Site site, Page page)
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

    private SearchResult ToSearchResult(Abstractions.Models.Site site, File file)
    {
      return new SearchResult()
      {
        Site = site,
        Url = $"files/{file.EncodeFileId()}",
        Title = System.IO.Path.GetFileName(file.Path),
        
        Scope = File.URN,
        SourceId = file.Id,
        ContentType = "text/html",
        PublishedDate = file.DateChanged ?? file.DateAdded,
        Type = "File",                
      };
    }
  }
}
