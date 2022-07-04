using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.EventHandlers;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace Nucleus.Core.Services
{
	/// <summary>
	/// Accepts event subscriptions by implementations of <![CDATA[ISystemEventHandler<TModel, TEvent>]]>, and event
	/// notifications (RaiseEvent).  Internal and custom implementations can acquire a reference to EventDispatcher
	/// from DI and raise or consume events.
	/// </summary>
	/// <remarks>
	/// Any number of event subscriptions may exist for any number of combinations of TModel and TEvent.  ISystemEventHandlers
	/// are invoked asynchronously in no particular order.
	/// </remarks>
	/// <example>
	/// Subscribing:
	/// <![CDATA[app.ApplicationServices.GetService<EventDispatcher>().Subscribe<User, Create>(new UserEventHandler());]]>
	/// Raising an Event:
	/// <![CDATA[EventDispatcher.RaiseEvent<User, Create>(myuser)]]>
	/// </example>
	public class EventDispatcher : IEventDispatcher
	{
		private ILogger<IEventDispatcher> Logger { get; }
		private IServiceProvider ServiceProvider { get; }
		private IHttpContextAccessor ContextAccessor { get; }

		public EventDispatcher(IServiceProvider serviceProvider, IHttpContextAccessor contextAccessor, ILogger<IEventDispatcher> logger)
		{
			this.ServiceProvider = serviceProvider;
			this.ContextAccessor = contextAccessor;
			this.Logger = logger;
		}

		/// <summary>
		/// Call RaiseEvent to submit an event to the Event Dispatcher.  Subscriptions of type TModel, TEvent will be 
		/// notified of the event.
		/// </summary>
		/// <typeparam name="TModel"></typeparam>
		/// <typeparam name="TEvent"></typeparam>
		/// <param name="item"></param>
		public void RaiseEvent<TModel, TEvent>(TModel item) 
			where TModel : class 
			where TEvent : class
		{
			IEnumerable<ISystemEventHandler<TModel, TEvent>> handlers;

			// If this.ContextAccessor is not null, use it to get scoped ISystemEventHandlers
			if (this.ContextAccessor.HttpContext != null)
			{
				handlers = this.ContextAccessor.HttpContext.RequestServices.GetServices<ISystemEventHandler<TModel, TEvent>>();
			}
			else
			{
				// Get singleton ISystemEventHandlers
				handlers = this.ServiceProvider.GetServices<ISystemEventHandler<TModel, TEvent>>();
			}

			if (handlers != null)
			{
				foreach (ISystemEventHandler<TModel, TEvent> handler in handlers)
				{
					// Subscription invocations are invoked asynchronously, and we do not await the conclusion of the invocation.
					Task task = Task.Run(() => Invoke<TModel, TEvent>(handler, item));
				}
			}

		}

		/// <summary>
		/// Subscription invoker.
		/// </summary>
		/// <typeparam name="TModel"></typeparam>
		/// <typeparam name="TEvent"></typeparam>
		/// <param name="handler"></param>
		/// <param name="item"></param>
		private void Invoke<TModel, TEvent>(ISystemEventHandler<TModel, TEvent> handler, TModel item) where TModel : class
		{
			try
			{
				// special case: provide a censored version of the User (no secrets or roles)
				if (typeof(TModel) == typeof(User))
				{
					User newitem = new() 
					{ 
						Id=(item as User).Id,
						Profile = (item as User).Profile,
						UserName = (item as User).UserName
					};

					item = newitem as TModel;
				}

				handler.Invoke(item);
			}
			catch (Exception e)
			{
				Logger.LogError(e, "Invoking event handler {0} for event {1}, model {2}.", handler.GetType(), typeof(TEvent), typeof(TModel));
			}
		}
	}
}
