using System;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Modules.$nucleus_extension_name$.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace $nucleus_extension_namespace$.DataProviders
{
	/// <summary>
	/// Module data provider.
	/// </summary>
	/// <remarks>
	/// This class implements the I$nucleus_extension_name$DataProvider interface, and inherits the base Nucleus entity framework data provider class.
	/// </remarks>
	public class $nucleus_extension_name$DataProvider : Nucleus.Data.EntityFramework.DataProvider, I$nucleus_extension_name$DataProviderDataProvider
	{
		protected IEventDispatcher EventManager { get; }
		protected new $nucleus_extension_name$DbContext Context { get; }

		public $nucleus_extension_name$DataProvider($nucleus_extension_name$DbContext context, IEventDispatcher eventManager, ILogger<$nucleus_extension_name$DataProvider> logger) : base(context, logger)
		{
			this.EventManager = eventManager;
			this.Context = context;
		}

		public async Task<$nucleus_extension_name$> Get(Guid id)
		{
			return await this.Context.$nucleus_extension_name$
				.Where($nucleus_extension_name_singular_lcase$ => $nucleus_extension_name_singular_lcase$ $nucleus_extension_name_singular_lcase$.Id == id)
				.Include($nucleus_extension_name_singular_lcase$ => $nucleus_extension_name_singular_lcase$ $nucleus_extension_name_singular_lcase$.Category)
				.Include($nucleus_extension_name_singular_lcase$ => $nucleus_extension_name_singular_lcase$ $nucleus_extension_name_singular_lcase$.File)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<IList<Document>> List(PageModule pageModule)
		{
			return await this.Context.$nucleus_extension_name$
				.Where($nucleus_extension_name_singular_lcase$ => $nucleus_extension_name_singular_lcase$ EF.Property<Guid>($nucleus_extension_name_singular_lcase$, "ModuleId") == pageModule.Id)
				.Include($nucleus_extension_name_singular_lcase$ => $nucleus_extension_name_singular_lcase$ $nucleus_extension_name_singular_lcase$.Category)
				.Include($nucleus_extension_name_singular_lcase$ => $nucleus_extension_name_singular_lcase$ $nucleus_extension_name_singular_lcase$.File)
				.AsNoTracking()
				.AsSingleQuery()
				.OrderBy($nucleus_extension_name_singular_lcase$ => $nucleus_extension_name_singular_lcase$ $nucleus_extension_name_singular_lcase$.SortOrder)
				.ToListAsync();
		}

		public async Task Save(PageModule pageModule, Document $nucleus_extension_name_singular_lcase$)
		{
			Action raiseEvent;

			Boolean isNew = !await this.Context.$nucleus_extension_name$.Where(existing => existing.Id == $nucleus_extension_name_singular_lcase$.Id).AnyAsync();

			this.Context.Attach($nucleus_extension_name_singular_lcase$);
			this.Context.Entry($nucleus_extension_name_singular_lcase$).Property("ModuleId").CurrentValue = pageModule.Id;

			if (isNew)
			{
				$nucleus_extension_name_singular_lcase$.SortOrder = await GetTopDocumentSortOrder(pageModule.Id) + 10;
				this.Context.Entry($nucleus_extension_name_singular_lcase$).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Document, Create>($nucleus_extension_name_singular_lcase$); });
			}
			else
			{
				this.Context.Entry($nucleus_extension_name_singular_lcase$).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Document, Update>($nucleus_extension_name_singular_lcase$); });
			}

			await this.Context.SaveChangesAsync();

			raiseEvent.Invoke();
		}

		public async Task Delete(Document $nucleus_extension_name_singular_lcase$)
		{
			this.Context.Remove($nucleus_extension_name_singular_lcase$);
			await this.Context.SaveChangesAsync<Document>();
		}


		private async Task<int> GetTopDocumentSortOrder(Guid moduleId)
		{
			Document $nucleus_extension_name_singular_lcase$ = await this.Context.$nucleus_extension_name$
				.Where($nucleus_extension_name_singular_lcase$ => $nucleus_extension_name_singular_lcase$ EF.Property<Guid>($nucleus_extension_name_singular_lcase$, "ModuleId") == moduleId)
				.OrderByDescending($nucleus_extension_name_singular_lcase$ => $nucleus_extension_name_singular_lcase$ $nucleus_extension_name_singular_lcase$.SortOrder)
				.FirstOrDefaultAsync();

			return $nucleus_extension_name_singular_lcase$ == null ? 10 : $nucleus_extension_name_singular_lcase$.SortOrder;
		}

	}
}