using System;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Modules.Documents.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Nucleus.Modules.Documents.DataProviders
{
	/// <summary>
	/// Links module data provider.
	/// </summary>
	/// <remarks>
	/// This class implements the ILinksDataProvider interface, and inherits the base Nucleus entity framework data provider class.
	/// </remarks>
	public class DocumentsDataProvider : Nucleus.Data.EntityFramework.DataProvider, IDocumentsDataProvider
	{
		protected IEventDispatcher EventManager { get; }
		protected new DocumentsDbContext Context { get; }

		public DocumentsDataProvider(DocumentsDbContext context, IEventDispatcher eventManager, ILogger<DocumentsDataProvider> logger) : base(context, logger)
		{
			this.EventManager = eventManager;
			this.Context = context;
		}

		public async Task<Document> Get(Guid id)
		{
			return await this.Context.Documents
				.Where(document => document.Id == id)
				.Include(document => document.Category)
				.Include(document => document.File)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<IList<Document>> List(PageModule pageModule)
		{
			return await this.Context.Documents
				.Where(document => EF.Property<Guid>(document, "ModuleId") == pageModule.Id)
				.Include(document => document.Category)
				.Include(document => document.File)
				.AsNoTracking()
				.AsSingleQuery()
				.OrderBy(document => document.SortOrder)
				.ToListAsync();
		}

		public async Task Save(PageModule pageModule, Document document)
		{
			Action raiseEvent;

			Boolean isNew = !await this.Context.Documents.Where(existing => existing.Id == document.Id).AnyAsync();

			this.Context.Attach(document);
			this.Context.Entry(document).Property("ModuleId").CurrentValue = pageModule.Id;

			if (isNew)
			{
				document.SortOrder = await GetTopDocumentSortOrder(pageModule.Id) + 10;
				this.Context.Entry(document).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Document, Create>(document); });
			}
			else
			{
				this.Context.Entry(document).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Document, Update>(document); });
			}

			await this.Context.SaveChangesAsync<Document>();

			raiseEvent.Invoke();
		}

		public async Task Delete(Document document)
		{
			this.Context.Remove(document);
			await this.Context.SaveChangesAsync<Document>();
		}


		private async Task<int> GetTopDocumentSortOrder(Guid moduleId)
		{
			Document document = await this.Context.Documents
				.Where(document => EF.Property<Guid>(document, "ModuleId") == moduleId)
				.OrderByDescending(document => document.SortOrder)
				.FirstOrDefaultAsync();

			return document == null ? 10 : document.SortOrder;
		}

	}
}