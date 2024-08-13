using System.Threading.Tasks;

namespace Nucleus.Abstractions.EventHandlers;

/// <summary>
/// This is the base interface for <see cref="IScopedSystemEventHandler{TModel, TEvent}"/> and <see cref="ISingletonSystemEventHandler{TModel, TEvent}"/>. You 
/// should implement one of those interfaces rather than this interface.
/// </summary>
/// <typeparam name="TModel">
/// The type of the data item which the event is for.  This can be any type and is the object type which is passed to the Invoke method.
/// </typeparam>
/// <typeparam name="TEvent">
/// The type of the event.  This can be any class.  The TEvent class is used to couple the subscription and 
/// ISystemEventHandler implementation to a specific type of event.  The TEvent type does not require any particular
/// implementation, it is used as a marker.
/// </typeparam>
/// <internal />
public interface ISystemEventHandler<TModel, TEvent>
{
  /// <summary>
  /// Raise an event
  /// </summary>
  /// <param name="item"></param>
  public abstract Task Invoke(TModel item);
}
