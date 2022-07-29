using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.EventHandlers
{
	/// <summary>
	/// Raises notification events.  Internal and custom implementations can acquire a reference to EventDispatcher
	/// from the dependency injection services container and raise or consume events.
	/// </summary>
	/// <remarks>
	/// To handle an event, create an implementation of <see cref="ISystemEventHandler{TModel, TEvent}"/> and add it to the dependency 
	/// injection services container in your <see href="https://www.nucleus-cms.com/developers/startup/">Startup Class</see>.  Event handlers 
	/// can be added as Scoped, Singleton or Transient.  If your event is expected to be triggered during a Http request, you can add your 
	/// handler with .AddScoped - scoped handlers can use references to other scoped classes (like <see cref="Models.Context"/>) to get 
	/// information about the current request.  
	/// 
	/// If your event is not expected to run during a Http request (from a scheduled task, or during startup), use .AddSingleton or .AddTransient 
	/// to add your event handler to the dependency injection services container.
	/// 
	/// Any number of event handlers may exist for any number of combinations of TModel and TEvent.  ISystemEventHandlers
	/// are invoked asynchronously in no particular order.
	/// 
	/// To raise an event, get a reference to the event dispatcher by including a parameter of type <see cref="IEventDispatcher"/> in your class constructor, and 
	/// call <see cref="IEventDispatcher.RaiseEvent{TModel, TEvent}(TModel)"/>. 
	/// 
	/// TModel can be any class type.  TEvent can also be any class type, but is normally a class created specifically to mark an event type, with no methods 
	/// or properties. Developers can use the <see cref="SystemEventTypes.Create"/>, <see cref="SystemEventTypes.Delete"/>
	/// or <see cref="SystemEventTypes.Update"/> events, or create specific classes for custom events.
	/// </remarks>
	/// <example>
	/// <![CDATA[//]]> Adding an event handler:
	/// <![CDATA[services.AddTransient<ISystemEventHandler<User, Create>, UserEventHandler>();]]>
	/// </example>
	/// <example>
	/// <![CDATA[//]]> Raising an Event:
	/// <![CDATA[eventDispatcher.RaiseEvent<User, Create>(myuser)]]>
	/// </example>
	public interface IEventDispatcher
	{
		/// <summary>
		/// Call RaiseEvent to submit an event to the Event Dispatcher.  The Invoke method will be called for all implementations of ISystemEventHandler{TModel, TEvent} in
		/// the services collection.
		/// </summary>
		/// <typeparam name="TModel"></typeparam>
		/// <typeparam name="TEvent"></typeparam>
		/// <param name="item"></param>
		public void RaiseEvent<TModel, TEvent>(TModel item) 
			where TModel : class
			where TEvent : class;

	}
}
