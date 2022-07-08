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
	/// To handle an event, create an implementation of <![CDATA[ISystemEventHandler<TModel, TEvent>]]> and add it to the dependency 
	/// injection services container.  Any number of event handlers may exist for any number of combinations of TModel and TEvent.  ISystemEventHandlers
	/// are invoked asynchronously in no particular order.
	/// 
	/// To raise an event, include IEventDispatcher in your constructor, and call <![CDATA[IEventDispatcher.RaiseEvent<TModel, TEvent>(instanceOfTModel);]]>
	/// 
	/// TModel can be any class type.  TEvent can also be any class type, but is normally a class created specifically to mark an event type, with no methods 
	/// or properties. The <see cref="SystemEventTypes"/> namespace contains classes for <see cref="SystemEventTypes.Create"/>, <see cref="SystemEventTypes.Delete"/>
	/// and <see cref="SystemEventTypes.Update"/> events, you can use these, or create your own for custom events.
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
