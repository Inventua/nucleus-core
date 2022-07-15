using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Threading;

namespace Nucleus.Data.EntityFramework
{
  /// <summary>
  /// SaveChangesInterceptor implementation which calls the relevant Nucleus data provider to parse exceptions and produce more friendly 
  /// error messages.
  /// </summary>
	public class ExceptionInterceptor : SaveChangesInterceptor
  {
    /// <summary>
    /// Calls the relevant Nucleus data provider to parse exceptions from Entity-Framework and produce more friendly error messages.
    /// </summary>
    /// <param name="eventData"></param>
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
      DbUpdateException dbUpdateException = eventData.Exception as DbUpdateException;
      Nucleus.Data.EntityFramework.DbContext dbContext = eventData.Context as Nucleus.Data.EntityFramework.DbContext;

      if (dbContext != null)
      {
        Nucleus.Data.EntityFramework.DbContextConfigurator contextConfig = (eventData.Context as Nucleus.Data.EntityFramework.DbContext).DbContextConfigurator;
        contextConfig.ParseException(dbUpdateException);
      }

      base.SaveChangesFailed(eventData);
    }

    /// <summary>
    /// Calls the relevant Nucleus data provider to parse exceptions from Entity-Framework and produce more friendly error messages.
    /// </summary>
    /// <param name="eventData"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = new CancellationToken())
    {
      SaveChangesFailed(eventData);

      return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }
  }
}
