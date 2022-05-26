using System;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using $nucleus_extension_namespace$.Models;
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
	public class $nucleus_extension_name$DataProvider : Nucleus.Data.EntityFramework.DataProvider, I$nucleus_extension_name$DataProvider
	{
		protected IEventDispatcher EventManager { get; }
		protected new $nucleus_extension_name$DbContext Context { get; }

		public $nucleus_extension_name$DataProvider($nucleus_extension_name$DbContext context, IEventDispatcher eventManager, ILogger<$nucleus_extension_name$DataProvider> logger) : base(context, logger)
		{
			this.EventManager = eventManager;
			this.Context = context;
		}

		public async Task<$nucleus_extension_modelname$> Get(Guid id)
		{
			return await this.Context.$nucleus_extension_modelname$s
				.Where($nucleus_extension_modelname_lcase$ => $nucleus_extension_modelname_lcase$.Id == id)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<IList<$nucleus_extension_modelname$>> List(PageModule pageModule)
		{
			return await this.Context.$nucleus_extension_modelname$s
				.Where($nucleus_extension_modelname_lcase$ => EF.Property<Guid>($nucleus_extension_modelname_lcase$, "ModuleId") == pageModule.Id)
				.AsNoTracking()
				.AsSingleQuery()
				.ToListAsync();
		}

		public async Task Save(PageModule pageModule, $nucleus_extension_modelname$ $nucleus_extension_modelname_lcase$)
		{
			Action raiseEvent;

			Boolean isNew = !await this.Context.$nucleus_extension_modelname$s
				.Where(existing => existing.Id == $nucleus_extension_modelname_lcase$.Id)
				.AsNoTracking()
				.AnyAsync();

			this.Context.Attach($nucleus_extension_modelname_lcase$);
			this.Context.Entry($nucleus_extension_modelname_lcase$).Property("ModuleId").CurrentValue = pageModule.Id;

			if (isNew)
			{
				this.Context.Entry($nucleus_extension_modelname_lcase$).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<$nucleus_extension_modelname$, Create >($nucleus_extension_modelname_lcase$); });
			}
			else
			{
				this.Context.Entry($nucleus_extension_modelname_lcase$).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<$nucleus_extension_modelname$, Update>($nucleus_extension_modelname_lcase$); });
			}

			await this.Context.SaveChangesAsync();

			raiseEvent.Invoke();
		}

		public async Task Delete($nucleus_extension_modelname$ $nucleus_extension_modelname_lcase$)
		{
			this.Context.Remove($nucleus_extension_modelname_lcase$);
			await this.Context.SaveChangesAsync<$nucleus_extension_modelname$>();
		}
	}
}