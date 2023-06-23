using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.DataProviders;
/// <summary>
/// Module data provider.
/// </summary>
/// <remarks>
/// This class implements the IDNNMigrationDataProvider interface, and inherits the base Nucleus entity framework data provider class.
/// </remarks>
public class DNNMigrationDataProvider : Nucleus.Data.EntityFramework.DataProvider
{
  protected IEventDispatcher EventManager { get; }
  protected new DNNMigrationDbContext Context { get; }

  public DNNMigrationDataProvider(DNNMigrationDbContext context, IEventDispatcher eventManager, ILogger<DNNMigrationDataProvider> logger) : base(context, logger)
  {
    this.EventManager = eventManager;
    this.Context = context;
  }

  public async Task<Models.MigrationLog> Get(Guid id)
  {
    return await this.Context.MigrationLogs
      .Where(log => log.Id == id)
      .AsNoTracking()
      .FirstOrDefaultAsync();
  }

  public async Task<IList<Models.MigrationLog>> List()
  {
    return await this.Context.MigrationLogs
      .AsNoTracking()
      .AsSingleQuery()
      .ToListAsync();
  }

  public async Task Save(Models.MigrationLog migrationLog)
  {
    Action raiseEvent;

    Boolean isNew = !await this.Context.MigrationLogs
      .Where(existing => existing.Id == migrationLog.Id)
      .AsNoTracking()
      .AnyAsync();

    this.Context.Attach(migrationLog);
    //this.Context.Entry(migrationLog).Property("ModuleId").CurrentValue = pageModule.Id;

    if (isNew)
    {
      this.Context.Entry(migrationLog).State = EntityState.Added;
      raiseEvent = new(() => { this.EventManager.RaiseEvent<Models.MigrationLog, Create>(migrationLog); });
    }
    else
    {
      this.Context.Entry(migrationLog).State = EntityState.Modified;
      raiseEvent = new(() => { this.EventManager.RaiseEvent<Models.MigrationLog, Update>(migrationLog); });
    }

    await this.Context.SaveChangesAsync();

    raiseEvent.Invoke();
  }

  public async Task Delete(Models.MigrationLog migrationLog)
  {
    this.Context.Remove(migrationLog);
    await this.Context.SaveChangesAsync<Models.MigrationLog>();
  }
}