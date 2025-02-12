﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Search
{
	/// <summary>
	/// Defines the interface for a search index manager.
	/// </summary>
	/// <remarks>
	/// Search index managers feed content to a search engine.  The interface for searching the index is provided by 
	/// a separate interface, <see cref="ISearchProvider"/>.
	/// </remarks>	
	public interface ISearchIndexManager
	{
    /// <summary>
    /// Return whether the underlying search system is available.
    /// </summary>
    /// <param name="site">Specifies the site.  Implementations can store per-site settings.</param>
    /// <returns></returns>
    /// <remarks>
    /// Implementation of this method is optional.  
    /// </remarks>
    public Task<Boolean> CanConnect(Site site) { return Task.FromResult(true); }

		/// <summary>
		/// Clear all entries from the index.
		/// </summary>
		/// <param name="site">Specifies the site whose index is to be cleared.</param>
		/// <returns></returns>
		public Task ClearIndex(Site site);

		/// <summary>
		/// Add the content specified by <paramref name="metadata"/> to the search index.
		/// </summary>
		/// <param name="metadata"></param>
		public Task Index(ContentMetaData metadata);

		/// <summary>
		/// Remove the content specified by <paramref name="metadata"/> from the search index, if it is present.
		/// </summary>
		/// <param name="metadata"></param>
		/// <remarks>
		/// If the content is not present or if the search index manager does not support removing content, this function should 
		/// return without error.  
		/// </remarks>
		public Task Remove(ContentMetaData metadata);
	}
}
