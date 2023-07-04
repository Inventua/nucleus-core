using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models;
using Nucleus.DNN.Migration.Models.Modules;
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

  ////public async Task<Models.MigrationHistory> Get(Guid id)
  ////{
  ////  return await this.Context.MigrationHistory
  ////    .Where(log => log.Id == id)
  ////    .AsNoTracking()
  ////    .FirstOrDefaultAsync();
  ////}

  ////public async Task<IList<Models.MigrationHistory>> List()
  ////{
  ////  return await this.Context.MigrationHistory
  ////    .AsNoTracking()
  ////    .AsSingleQuery()
  ////    .ToListAsync();
  ////}

  ////public async Task Save(Models.MigrationHistory migrationLog)
  ////{
  ////  Action raiseEvent;

  ////  Boolean isNew = !await this.Context.MigrationHistory
  ////    .Where(existing => existing.Id == migrationLog.Id)
  ////    .AsNoTracking()
  ////    .AnyAsync();

  ////  this.Context.Attach(migrationLog);
  ////  //this.Context.Entry(migrationLog).Property("ModuleId").CurrentValue = pageModule.Id;

  ////  if (isNew)
  ////  {
  ////    this.Context.Entry(migrationLog).State = EntityState.Added;
  ////    raiseEvent = new(() => { this.EventManager.RaiseEvent<Models.MigrationHistory, Create>(migrationLog); });
  ////  }
  ////  else
  ////  {
  ////    this.Context.Entry(migrationLog).State = EntityState.Modified;
  ////    raiseEvent = new(() => { this.EventManager.RaiseEvent<Models.MigrationHistory, Update>(migrationLog); });
  ////  }

  ////  await this.Context.SaveChangesAsync();

  ////  raiseEvent.Invoke();
  ////}

  ////public async Task Delete(Models.MigrationHistory migrationLog)
  ////{
  ////  this.Context.Remove(migrationLog);
  ////  await this.Context.SaveChangesAsync<Models.MigrationHistory>();
  ////}
  ///

  #region "    Documents    "
  ////public async Task<Document> GetDocumentByTitle(Guid moduleId, string title)
  ////{
  ////  return await this.Context.Documents
  ////    .Where(document => EF.Property<Guid>(document, "ModuleId") == moduleId && document.Title == title)
  ////    .Include(document => document.Category)
  ////    .Include(document => document.File)
  ////    .AsNoTracking()
  ////    .FirstOrDefaultAsync();
  ////}

  ////public async Task SaveDocument(PageModule pageModule, Document document)
  ////{
  ////  Action raiseEvent;

  ////  Boolean isNew = !await this.Context.Documents.Where(existing => existing.Id == document.Id).AnyAsync();

  ////  this.Context.Attach(document);
  ////  this.Context.Entry(document).Property("ModuleId").CurrentValue = pageModule.Id;

  ////  if (isNew)
  ////  {
  ////    if (document.SortOrder == 0)
  ////    {
  ////      document.SortOrder = await GetTopDocumentSortOrder(pageModule.Id) + 10;
  ////    }

  ////    this.Context.Entry(document).State = EntityState.Added;
  ////    raiseEvent = new(() => { this.EventManager.RaiseEvent<Document, Create>(document); });
  ////  }
  ////  else
  ////  {
  ////    this.Context.Entry(document).State = EntityState.Modified;
  ////    raiseEvent = new(() => { this.EventManager.RaiseEvent<Document, Update>(document); });
  ////  }

  ////  await this.Context.SaveChangesAsync<Document>();

  ////  raiseEvent.Invoke();
  ////}


  ////private async Task<int> GetTopDocumentSortOrder(Guid moduleId)
  ////{
  ////  Document document = await this.Context.Documents
  ////    .Where(document => EF.Property<Guid>(document, "ModuleId") == moduleId)
  ////    .OrderByDescending(document => document.SortOrder)
  ////    .FirstOrDefaultAsync();

  ////  return document == null ? 10 : document.SortOrder;
  ////}
  #endregion
}