using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Abstractions.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.EventHandlers;

/// <summary>
/// Extension methods for adding Nucleus system event handlers.
/// </summary>
public static class EventHandlerExtensions
{
  /// <summary>
  /// Add a scoped system event handler of <typeparamref name="THandler"/> for the specified <typeparamref name="TModel"/>, <typeparamref name="TEvent"/>.
  /// </summary>
  /// <typeparam name="TModel"></typeparam>
  /// <typeparam name="TEvent"></typeparam>
  /// <typeparam name="THandler"></typeparam>
  /// <returns></returns>
  /// <remarks>
  /// Scoped system event handlers can only be used from within a controller action, because the event dispatcher requires 
  /// a current HttpContext.
  /// </remarks>
  public static IServiceCollection AddScopedSystemEventHandler<TModel, TEvent, THandler>(this IServiceCollection services)
    where THandler : class, IScopedSystemEventHandler<TModel, TEvent>
  {
    services.AddScoped<IScopedSystemEventHandler<TModel, TEvent>, THandler>();        
    return services;
  }

  /// <summary>
  /// Add a singleton system event handler of <typeparamref name="THandler"/> for the specified <typeparamref name="TModel"/>, <typeparamref name="TEvent"/>.
  /// </summary>
  /// <typeparam name="TModel"></typeparam>
  /// <typeparam name="TEvent"></typeparam>
  /// <typeparam name="THandler"></typeparam>
  /// <remarks>
  /// Singleton system event handlers are used when an event is raised from a scheduled task, or in other cases where there 
  /// isn't a current HttpContext.
  /// </remarks>
  /// <returns></returns>
  public static IServiceCollection AddSingletonSystemEventHandler<TModel, TEvent, THandler>(this IServiceCollection services)
    where THandler : class, ISingletonSystemEventHandler<TModel, TEvent>
  {
    services.AddSingleton<ISingletonSystemEventHandler<TModel, TEvent>, THandler>();
    return services;
  }


}
