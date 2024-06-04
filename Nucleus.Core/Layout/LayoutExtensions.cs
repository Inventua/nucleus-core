using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Layout;
using Nucleus.Abstractions.Models;

namespace Nucleus.Core.Layout;

public static class LayoutExtensions
{
  /// <summary>
  /// Add layout classes to the dependency injection container
  /// </summary>
  /// <param name="services"></param>
  /// <returns></returns>
  public static IServiceCollection AddNucleusLayout(this IServiceCollection services)
  {
    // Layout components require a scoped context object
    services.AddScoped<Context>();

    // Container processing requires a scoped ContainerContext object
    services.AddScoped<ContainerContext>();

    services.AddSingleton<PageRoutingMiddleware>();
    services.AddSingleton<ModuleRoutingMiddleware>();
    services.AddSingleton<IModuleContentRenderer, ModuleContentRenderer>();

    return services;
  }
}
