using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Search;

/// <summary>
/// Provides functions to manage search index history.
/// </summary>
/// <remarks>
/// Search index history is used to track when search content items were indexed and is used to reduce the processing overhead for the search 
/// feeds by not submitting updates to search indexes when data has not changed.
/// <seealso cref="IContentMetaDataProducer"/> implementations can use this class to get the last-indexed date for entities and
/// skip submitting them to search indexes if their content has not changed.
/// </remarks>
public interface ISearchIndexHistoryManager
{
  /// <summary>
  /// Create or update a search index history item.
  /// </summary>
  /// <param name="history"></param>
  /// <returns></returns>
  /// <internal>
  /// This function is called by the core search index feeder and does not need to be called by <seealso cref="IContentMetaDataProducer"/> implementations.
  /// </internal>
  abstract Task Save(SearchIndexHistory history);

  /// <summary>
  /// Retrieve a search index history item.
  /// </summary>
  /// <param name="siteId"></param>
  /// <param name="scope"></param>
  /// <param name="sourceId"></param>
  /// <returns></returns>
  /// <remarks>
  /// <seealso cref="IContentMetaDataProducer"/> implementations use this function to retrieve the specified item's last indexed date.
  /// </remarks>
  abstract Task<SearchIndexHistory> Get(Guid siteId, string scope, Guid sourceId);

  /// <summary>
  /// Delete the specified search index history item.
  /// </summary>
  /// <param name="siteId"></param>
  /// <param name="scope"></param>
  /// <param name="sourceId"></param>
  /// <returns></returns>
  abstract Task Delete(Guid siteId, string scope, Guid sourceId);

  /// <summary>
  /// Delete all search index history for the site specified by <paramref name="siteId"/>.
  /// </summary>
  /// <param name="siteId"></param>
  /// <returns></returns>
  abstract Task Delete(Guid siteId);
}
