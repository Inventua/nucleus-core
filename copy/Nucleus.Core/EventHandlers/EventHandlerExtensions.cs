using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Nucleus.Core;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Core.EventHandlers.Abstractions.SystemEventTypes;
using Nucleus.Core.EventHandlers.Abstractions;

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
		/// Any <see cref="ISystemEventHandler"/> implementations added to DI are automatically called by the <see cref="EventDispatcher"/> when their
		/// corresponding TModel/TEvent is raised.
		/// </remarks>
		public static IServiceCollection AddCoreEventHandlers(this IServiceCollection services)
		{
			services.AddTransient<ISystemEventHandler<User, Create>, UserEventHandler>();
			services.AddTransient<ISystemEventHandler<User, Create>, UserEventHandler2>();
			services.AddTransient<ISystemEventHandler<Page, Create>, PageEventHandler>();
			
			return services;
		}

	}

}
