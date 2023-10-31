using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Search;

namespace Nucleus.Core.Search
{
  /// <summary>
  /// Provides functions to manage database data for <see cref="SearchIndexHistory"/>s.
  /// </summary>
  /// <remarks>
  /// <seealso cref="IContentMetaDataProducer"/> implementations can use this class to get the last-indexed date for entities and
  /// skip submitting them to search indexes if their content has not changed.
  /// </remarks>
  public class SearchIndexHistoryManager : ISearchIndexHistoryManager
  {
    private IDataProviderFactory DataProviderFactory { get; }

    public SearchIndexHistoryManager(IDataProviderFactory dataProviderFactory)
    {
      DataProviderFactory = dataProviderFactory;
    }

    /// <summary>
    /// Retrieve an existing <see cref="SearchIndexHistory"/> from the database.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<SearchIndexHistory> Get(Guid siteId, string scope, Guid sourceId)
    {
      using (ISearchIndexHistoryDataProvider provider = DataProviderFactory.CreateProvider<ISearchIndexHistoryDataProvider>())
      {
        return await provider.GetSearchIndexHistory(siteId, scope, sourceId);
      };
    }


    /// <summary>
    /// Create or update the specified <see cref="SearchIndexHistory"/>.
    /// </summary>
    /// <param name="SearchIndexHistory"></param>
    public async Task Save(SearchIndexHistory history)
    {
      using (ISearchIndexHistoryDataProvider provider = DataProviderFactory.CreateProvider<ISearchIndexHistoryDataProvider>())
      {
        await provider.SaveSearchIndexHistory(history);
      }
    }

    /// <summary>
    /// Delete the specified <see cref="SearchIndexHistory"/> from the database.
    /// </summary>
    /// <param name="SearchIndexHistory"></param>
    public async Task Delete(Guid siteId, string scope, Guid sourceId)
    {
      using (ISearchIndexHistoryDataProvider provider = DataProviderFactory.CreateProvider<ISearchIndexHistoryDataProvider>())
      {
        await provider.DeleteSearchIndexHistory(siteId, scope, sourceId);
      }
    }

    /// <summary>
    /// Delete all <see cref="SearchIndexHistory"/> entries from the database for the site specified by <paramref name="siteId"/>.
    /// </summary>
    /// <param name="SearchIndexHistory"></param>
    public async Task Delete(Guid siteId)
    {
      using (ISearchIndexHistoryDataProvider provider = DataProviderFactory.CreateProvider<ISearchIndexHistoryDataProvider>())
      {
        await provider.DeleteSearchIndexHistory(siteId);
      }
    }

  }
}
