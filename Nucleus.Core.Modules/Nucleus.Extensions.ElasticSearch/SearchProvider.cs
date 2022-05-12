using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Search;

namespace Nucleus.Extensions.ElasticSearch
{
	public class SearchProvider : ISearchProvider
	{
		private ISiteManager SiteManager { get; }
		private IListManager ListManager { get; }
		private IRoleManager RoleManager { get; }


		public SearchProvider(ISiteManager siteManager, IListManager listManager, IRoleManager roleManager)
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
				throw new InvalidOperationException($"The Elastic search server url is not set for site '{query.Site.Name}'.");
			}

			if (String.IsNullOrEmpty(settings.IndexName))
			{
				throw new InvalidOperationException($"The Elastic search index name is not set for site '{query.Site.Name}'.");
			}

			ElasticSearchRequest request = new(new System.Uri(settings.ServerUrl), settings.IndexName);

			if (query.Boost == null)
			{
				query.Boost = settings.Boost;
			}

			Nest.ISearchResponse<ElasticSearchDocument> result = request.Search(query);

			return new SearchResults()
			{
				Results = await ToSearchResults(result.Documents),
				Total = result.Total
			};
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

			ElasticSearchRequest request = new(new System.Uri(settings.ServerUrl), settings.IndexName);
			Nest.ISearchResponse<ElasticSearchDocument> result = request.Suggest(query, 5);

			return new SearchResults()
			{
				Results = await ToSearchResults(result.Documents),
				Total = result.Total
			};
		}

		private async Task<IEnumerable<SearchResult>> ToSearchResults(IReadOnlyCollection<ElasticSearchDocument> documents)
		{
			List<SearchResult> results = new();

			foreach (ElasticSearchDocument document in documents)
			{
				results.Add(await ToSearchResult(document));
			}

			return results;
		}

		private async Task<SearchResult> ToSearchResult(ElasticSearchDocument document)
		{
			return new SearchResult()
			{
				Score = document.Score,

				Site = await this.SiteManager.Get(document.SiteId),
				Url = document.Url,
				Title = document.Title,
				Summary = document.Summary,

				Scope = document.Scope,
				SourceId = document.SourceId,
				ContentType = document.ContentType,
				PublishedDate = document.PublishedDate,

				Size = document.Size,
				Keywords = document.Keywords,
				Categories = await ToCategories(document.Categories),
				Roles = await ToRoles(document.Roles)
			};
		}

		private async Task<IEnumerable<ListItem>> ToCategories(IEnumerable<Guid> idList)
		{
			List<ListItem> results = new();

			foreach (Guid id in idList)
			{
				results.Add(await this.ListManager.GetListItem(id));
			}

			return results.Where(result => result != null);
		}

		private async Task<IEnumerable<Role>> ToRoles(IEnumerable<Guid> idList)
		{
			List<Role> results = new();

			foreach (Guid id in idList)
			{
				results.Add(await this.RoleManager.Get(id));
			}

			return results.Where(result => result != null);
		}
	}
}
