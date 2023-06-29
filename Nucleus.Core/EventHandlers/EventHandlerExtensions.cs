using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Core;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;

namespace Nucleus.Core.EventHandlers
{
	public static class EventHandlerExtensions
	{
		/// <summary>
		/// Add Nucleus system event handlers to DI.
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		/// <remarks>
		/// Any <see cref="ISystemEventHandler"/> implementations added to DI are automatically called by the <see cref="IEventDispatcher"/> when their
		/// corresponding TModel/TEvent is raised.
		/// </remarks>
		public static IServiceCollection AddCoreEventHandlers(this IServiceCollection services)
		{
			services.AddScopedSystemEventHandler<User, Create, UserEventHandler>();
			////services.AddSingletonSystemEventHandler<Page, Create, PageEventHandler>();
			////services.AddSingletonSystemEventHandler<ScheduledTask, Update, ScheduledTaskEventHandler>();

			return services;
		}

	}

}