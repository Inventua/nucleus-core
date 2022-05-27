using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Search
{
	/// <summary>
	/// Specifies search query parameters.
	/// </summary>
	public class SearchQuery
	{
		/// <summary>
		/// Specifies the site to show results for.
		/// </summary>
		public Site Site { get; set; }

		/// <summary>
		/// Specifies the roles that the current user belongs to.
		/// </summary>
		/// <remarks>
		/// This value is used to filter the results so that only results which the user can view are shown.
		/// </remarks>
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
		public IEnumerable<Role>? Roles { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

		/// <summary>
		/// Specifies the search term to search for.
		/// </summary>
		public string SearchTerm { get; set; }

		/// <summary>
		/// Specifies whether all search terms must be present in each result (true), or whether any search terms must be present (false).
		/// </summary>
		public Boolean StrictSearchTerms { get; set; }

		/// <summary>
		/// Specifies scopes to include.
		/// </summary>
		public List<string> IncludedScopes { get; set; } = new List<string>();

		/// <summary>
		/// Specifies scopes to include.
		/// </summary>
		/// <remarks>
		/// Search meta-data producers set the scope for their search content items, and the search index scope indicates the content source, for example the scope for a nucleus page is urn:nucleus:entities:page
		/// </remarks>
		public List<string> ExcludedScopes { get; set; } = new List<string>();

		/// <summary>
		/// Specifies which page of search results to return.
		/// </summary>
		public Models.Paging.PagingSettings PagingSettings { get; set; } = new();

		/// <summary>
		/// Specifies query boost settings. 
		/// </summary>
		/// <remarks>
		/// Boost settings are used to prioritize field content in search results.  Search providers which do not implement search-time boost can
		/// ignore these settings.  The default boost for all fields is 1.  
		/// </remarks>
		public BoostSettings Boost { get; set; }

		/// <summary>
		/// Specifies boost settings.
		/// </summary>
		public class BoostSettings
		{
			/// <summary>
			/// Title boost.
			/// </summary>
			public Double Title { get; set; } = 1.0;

			/// <summary>
			/// Summary boost.
			/// </summary>
			public Double Summary {get;set;} = 1.0;

			/// <summary>
			/// Categories boost.
			/// </summary>
			public Double Categories {get;set;} = 1.0;

			/// <summary>
			/// Keywords boost.
			/// </summary>
			public Double Keywords {get;set;} = 1.0;

			/// <summary>
			/// Content boost.
			/// </summary>
			public Double Content { get; set; } = 1.0;

			/// <summary>
			/// Attachment author boost.
			/// </summary>
			/// <remarks>
			/// Attachment fields are extracted from the meta-data for a document.
			/// </remarks>
			public Double AttachmentAuthor {get;set;} = 1.0;

			/// <summary>
			/// Attachment keywords boost.
			/// </summary>
			/// <remarks>
			/// Attachment fields are extracted from the meta-data for a document.
			/// </remarks>
			public Double AttachmentKeywords {get;set;} = 1.0;

			/// <summary>
			/// Attachment name boost.
			/// </summary>
			/// <remarks>
			/// Attachment fields are extracted from the meta-data for a document.
			/// </remarks>
			public Double AttachmentName {get;set;} = 1.0;

			/// <summary>
			/// Attachment title boost.
			/// </summary>
			/// <remarks>
			/// Attachment fields are extracted from the meta-data for a document.
			/// </remarks>
			public Double AttachmentTitle {get;set;} = 1.0;
			
		}

	}
}
