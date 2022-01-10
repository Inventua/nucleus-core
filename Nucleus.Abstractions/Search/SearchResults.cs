using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Search
{
	/// <summary>
	/// Represents the search results for a search query.
	/// </summary>
	public class SearchResults
	{
		/// <summary>
		/// A list of search results.
		/// </summary>
		/// <remarks>
		/// The results list contains results for the requested page.
		/// </remarks>
		public IEnumerable<SearchResult> Results { get; set; }

		/// <summary>
		/// Total number of results matching the search query.
		/// </summary>
		/// <remarks>
		/// When a page of results is requested, the result contains the requested page only.  The total property specifies the total
		/// number of available results and is used by paging controls.
		/// </remarks>
		public long Total { get; set; }
	}
}
