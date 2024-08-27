using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Search;
using System.ComponentModel;
using Nest;

namespace Nucleus.Extensions.ElasticSearch
{
	[DisplayName("Elastic Search Provider")]
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

			ElasticSearchRequest request = new(new System.Uri(settings.ServerUrl), settings.IndexName, settings.Username, ConfigSettings.DecryptPassword(query.Site, settings.EncryptedPassword), settings.CertificateThumbprint);

			if (query.Boost == null)
			{
				query.Boost = settings.Boost;
			}

			Nest.ISearchResponse<ElasticSearchDocument> result = await request.Search(query);

			return new SearchResults()
			{
				Results = await ToSearchResults(result),
        MaxScore = result.MaxScore,
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

			ElasticSearchRequest request = new(new System.Uri(settings.ServerUrl), settings.IndexName, settings.Username, ConfigSettings.DecryptPassword(query.Site, settings.EncryptedPassword), settings.CertificateThumbprint);
			Nest.ISearchResponse<ElasticSearchDocument> result = await request.Suggest(query);

      return new SearchResults()
      {
        Results = await ToSearchResults(result),
        MaxScore = result.MaxScore,
				Total = result.Total
			};
		}

		private async Task<IEnumerable<SearchResult>> ToSearchResults(Nest.ISearchResponse<ElasticSearchDocument> searchResults)
		{
			List<SearchResult> results = new();

			foreach (ElasticSearchDocument document in searchResults.Documents)
			{
				results.Add(await ToSearchResult(document, searchResults.Hits.Where(hit => hit.Source.Id == document.Id)));
			}

			return results;
		}

		private async Task<SearchResult> ToSearchResult(ElasticSearchDocument document, IEnumerable<IHit<ElasticSearchDocument>> hits)
		{
			return new SearchResult()
			{
				Score = document.Score,

        MatchedTerms = hits?
          .SelectMany(highlight => highlight.Highlight)
          .SelectMany(highlight => highlight.Value)
          .SelectMany(highlightValue => ExtractHighlightedTerms(highlightValue))
          .Where(value => !String.IsNullOrEmpty(value))
          .Distinct()
          .ToList(),

        Site = await this.SiteManager.Get(document.SiteId),
				Url = document.Url,
				Title = document.Title,
				Summary = document.Summary,

				Scope = document.Scope,
        Type = document.Type,
				SourceId = document.SourceId,
				ContentType = document.ContentType,
				PublishedDate = document.PublishedDate,

				Size = document.Size,
				Keywords = document.Keywords,
				Categories = await ToCategories(document.Categories),
				Roles = await ToRoles(document.Roles)
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
          results.Add(match.Groups["term"].Value);
        }
      }
      

      return results;
    }

    private async Task<IEnumerable<ListItem>> ToCategories(IEnumerable<Guid> idList)
		{
			if (idList == null) return default;

			List<ListItem> results = new();

			foreach (Guid id in idList)
			{
				results.Add(await this.ListManager.GetListItem(id));
			}

			return results.Where(result => result != null);
		}

		private async Task<IEnumerable<Role>> ToRoles(IEnumerable<Guid> idList)
		{
			if (idList == null) return default;

			List<Role> results = new();

			foreach (Guid id in idList)
			{
				results.Add(await this.RoleManager.Get(id));
			}

			return results.Where(result => result != null);
		}
	}
}
