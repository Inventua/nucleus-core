using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Search
{
	/// <summary>
	/// Defines the interface for a search provider.
	/// </summary>
	/// <remarks>
	/// Search providers retrieve search results from a search engine.  The interface for search index management is provided by 
	/// a separate interface, <see cref="ISearchIndexManager"/>.
	/// </remarks>	
	public interface ISearchProvider
	{
		/// <summary>
		/// Return results matching the values specified by <paramref name="query"/>.
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		public Task<SearchResults> Search(SearchQuery query);

		/// <summary>
		/// Return search suggestions matching the values specified by <paramref name="query"/>.
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		/// <remarks>
		/// Implementations of this interface which do not provide search suggestions can throw a NotImplementedException.
		/// </remarks>
		public Task<SearchResults> Suggest(SearchQuery query);
	}
}
