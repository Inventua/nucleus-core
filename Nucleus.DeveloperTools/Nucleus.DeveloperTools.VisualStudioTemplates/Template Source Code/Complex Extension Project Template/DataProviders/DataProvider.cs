using System;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using $nucleus.extension.namespace$.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace $nucleus.extension.namespace$.DataProviders
{
	/// <summary>
	/// Module data provider.
	/// </summary>
	/// <remarks>
	/// This class implements the I$nucleus.extension.name$DataProvider interface, and inherits the base Nucleus entity framework data provider class.
	/// </remarks>
	public class $nucleus.extension.name$DataProvider : Nucleus.Data.EntityFramework.DataProvider, I$nucleus.extension.name$DataProvider
	{
		protected IEventDispatcher EventManager { get; }
		protected new $nucleus.extension.name$DbContext Context { get; }

		public $nucleus.extension.name$DataProvider($nucleus.extension.name$DbContext context, IEventDispatcher eventManager, ILogger<$nucleus.extension.name$DataProvider> logger) : base(context, logger)
		{
			this.EventManager = eventManager;
			this.Context = context;
		}

		public async Task<$nucleus.extension.model_class_name$> Get(Guid id)
		{
			return await this.Context.$nucleus.extension.model_class_name$s
				.Where($nucleus.extension.model_class_name.camelcase$ => $nucleus.extension.model_class_name.camelcase$.Id == id)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<IList<$nucleus.extension.model_class_name$>> List(PageModule pageModule)
		{
			return await this.Context.$nucleus.extension.model_class_name$s
				.Where($nucleus.extension.model_class_name.camelcase$ => EF.Property<Guid>($nucleus.extension.model_class_name.camelcase$, "ModuleId") == pageModule.Id)
				.AsNoTracking()
				.AsSingleQuery()
				.ToListAsync();
		}

		public async Task Save(PageModule pageModule, $nucleus.extension.model_class_name$ $nucleus.extension.model_class_name.camelcase$)
		{
			Action raiseEvent;

			Boolean isNew = !await this.Context.$nucleus.extension.model_class_name$s
				.Where(existing => existing.Id == $nucleus.extension.model_class_name.camelcase$.Id)
				.AsNoTracking()
				.AnyAsync();

			this.Context.Attach($nucleus.extension.model_class_name.camelcase$);
			this.Context.Entry($nucleus.extension.model_class_name.camelcase$).Property("ModuleId").CurrentValue = pageModule.Id;

			if (isNew)
			{
				this.Context.Entry($nucleus.extension.model_class_name.camelcase$).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<$nucleus.extension.model_class_name$, Create >($nucleus.extension.model_class_name.camelcase$); });
			}
			else
			{
				this.Context.Entry($nucleus.extension.model_class_name.camelcase$).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<$nucleus.extension.model_class_name$, Update>($nucleus.extension.model_class_name.camelcase$); });
			}

			await this.Context.SaveChangesAsync();

			raiseEvent.Invoke();
		}

		public async Task Delete($nucleus.extension.model_class_name$ $nucleus.extension.model_class_name.camelcase$)
		{
			this.Context.Remove($nucleus.extension.model_class_name.camelcase$);
			await this.Context.SaveChangesAsync<$nucleus.extension.model_class_name$>();
		}
	}
}