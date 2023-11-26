using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Search;
using Nucleus.Data.Common;

namespace Nucleus.Core.DataProviders
{
  /// Provides create, read, update and delete functionality for the <see cref="SearchIndexHistory"/> class.
  internal interface ISearchIndexHistoryDataProvider : IDisposable
  {
    abstract Task SaveSearchIndexHistory(SearchIndexHistory history);
    abstract Task<SearchIndexHistory> GetSearchIndexHistory(Guid siteId, string scope, Guid sourceId);
    abstract Task DeleteSearchIndexHistory(Guid siteId, string scope, Guid sourceId);
    abstract Task DeleteSearchIndexHistory(Guid siteId);

  }
}
