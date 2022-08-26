using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Search;
using Nucleus.Extensions;
using DocumentFormat.OpenXml.Wordprocessing;
using System.ComponentModel;

namespace Nucleus.Modules.Search
{
	[DisplayName("Basic Page Search Provider")]
	public class BasicSearchProvider : ISearchProvider
	{
		private ISiteManager SiteManager { get; }
		private IPageManager PageManager { get; }
		

		public BasicSearchProvider(ISiteManager siteManager, IListManager listManager, IRoleManager roleManager, IPageManager pageManager)
		{
			this.SiteManager = siteManager;
			this.PageManager = pageManager;
		}

		public async Task<SearchResults> Search(SearchQuery query)
		{
			if (query.Site == null)
			{
				throw new ArgumentException($"The site property is required.", nameof(query.Site));
			}

			Abstractions.Models.Paging.PagedResult<Page> pages = await this.PageManager.Search(query.Site,query.SearchTerm,query.PagingSettings);

			return new SearchResults()
			{
				Results = await ToSearchResults(pages),
				Total = pages.TotalPages
			};
		}

		public Task<SearchResults> Suggest(SearchQuery query)
		{
			return Task.FromResult(new SearchResults()
			{
				Total = 0
			});
		}

		private async Task<IEnumerable<SearchResult>> ToSearchResults(Abstractions.Models.Paging.PagedResult<Page> pages)
		{
			List<SearchResult> results = new();

			foreach (Page page in pages.Items)
			{
				results.Add(await ToSearchResult(page));
			}

			return results;
		}

		private async Task<SearchResult> ToSearchResult(Page page)
		{
			return new SearchResult()
			{
				Score = 0,

				Site = await this.SiteManager.Get(page.SiteId),
				Url = page.DefaultPageRoute().Path,
				Title = page.Title,
				Summary = page.Description,

				Scope = Page.URN,
				SourceId = page.Id,
				ContentType = "text/html",
				PublishedDate = page.DateChanged ?? page.DateAdded,

				Keywords = page.Keywords?.Split(',')
				//Roles = page.Permissions.Where(permission=>permission.IsValid(HttpContext.) && permission.IsPageViewPermission()).Select(permission => permission.Role)
			};
		}


	}
}
