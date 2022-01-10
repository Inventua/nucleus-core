using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.EventHandlers
{
	/// <summary>
	/// System event handler abstract class.  Event handlers implement this class, and are added to the
	/// dependency injection services collection with a call to services.AddTransient.
	/// </summary>
	/// <typeparam name="TModel">The type of the data item which the event is for.  This can be any type.</typeparam>
	/// <typeparam name="TEvent">
	/// The type of the event.  This can be any class.  The TEvent class is used to couple the subscription and 
	/// ISystemEventHandler implementation to a specific type of event.  The TEvent type does not require any particular
	/// implementation, it is used as a marker.
	/// </typeparam>
	/// <remarks>
	/// Implementations of ISystemEventHandler handle a single model type and event type represented by <typeparamref name="TEvent"/> and <typeparamref name="TModel"/>.
	/// </remarks>
	public interface ISystemEventHandler<TModel, TEvent>
	{
		/// <summary>
		/// Raise an event
		/// </summary>
		/// <param name="item"></param>
		public abstract Task Invoke(TModel item);
	}

}
