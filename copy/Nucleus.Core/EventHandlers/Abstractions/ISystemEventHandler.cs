using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Core.EventHandlers.Abstractions
{
	/// <summary>
	/// System event handler abstract class.  Event handlers implement this class, and are added to the
	/// .Subscriptions collection with a call to EventDispatcher.Subscribe.
	/// </summary>
	/// <typeparam name="TModel">The type of the data item which the event is for.  This can be any type.</typeparam>
	/// <typeparam name="TEvent">
	/// The type of the event.  This can be any class.  The TEvent class is used to couple the subscription and 
	/// ISystemEventHandler implementation to a specific type of event.  The TEvent type does not require any particular
	/// implementation, it is used as a marker.
	/// </typeparam>
	/// <remarks>
	/// Implementations of ISystemEventHandler are tightly coupled to a a single model type and event type.
	/// </remarks>
	public interface ISystemEventHandler<TModel, TEvent>
	{
		public abstract void Invoke(TModel item);
	}

}
