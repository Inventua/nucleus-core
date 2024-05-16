using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Core.Logging;
using Nucleus.Extensions.Logging;

namespace Nucleus.Core.Plugins;

/// <summary>
/// Provides methods used to set up Nucleus Core plugins.
/// </summary>
public static class PluginExtensions
{
  /// <summary>
  /// Add extension controllers, compiled razor views, scheduled tasks and IPortable implementations.
  /// </summary>
  /// <param name="builder"></param>
  /// <param name="contentRootPath"></param>
  /// <returns></returns>
  /// <internal>
  /// This method is intended for use by Nucleus.Web.Startup.
  /// </internal>
  public static IMvcBuilder AddPlugins(this IMvcBuilder builder, string contentRootPath)
  {
    builder.AddExternalControllers();
    builder.AddCompiledRazorViews();
    builder.AddScheduledTasks();
    builder.AddPortable();

    builder.Services.Configure<RazorViewEngineOptions>(options =>
    {
      options.ViewLocationExpanders.Add(new ExtensionViewLocationExpander());      
    });

    ConfigureRazorRuntimeCompilation(builder);

    return builder;
  }

  /// <summary>
  /// Add all assemblies in sub-folders of /Extensions/*/bin as additional reference paths for Razor runtime complication.
  /// </summary>
  /// <param name="builder"></param>
  /// <returns></returns>
  /// <remarks>
  /// Razor runtime compilation won't use types from assemblies which aren't the assembly containing the controller class 
  /// unless it is specifically told to do so.  This function adds all assemblies in /Extensions/*/bin/** as 
  /// additional reference paths.
  /// </remarks>
  private static IMvcBuilder ConfigureRazorRuntimeCompilation(this IMvcBuilder builder)
  {
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation.MvcRazorRuntimeCompilationOptions>(options =>
    {
      string extensionsFolder = Nucleus.Abstractions.Models.Configuration.FolderOptions.GetExtensionsFolderStatic(false);
      Dictionary<string, string> references = new(StringComparer.OrdinalIgnoreCase);

      foreach (Assembly assembly in AssemblyLoader.ListAssemblies())
      {
        string assemblyPath = Nucleus.Abstractions.Models.Configuration.FolderOptions.NormalizePath(assembly.Location);

        if (assemblyPath.StartsWith(extensionsFolder, StringComparison.OrdinalIgnoreCase))
        {
          // Note:  AdditionalReferencePaths expects the file name of an assembly, not a directory name.
          if (!references.ContainsKey(assembly.GetName().FullName))
          {
            references.Add(assembly.GetName().FullName, assemblyPath);
          }
          else if (assemblyPath != references[assembly.GetName().FullName])
          {
            builder.Logger().LogInformation("Skipped adding Razor runtime compliation additional reference path '{path}' because another copy exists at '{existing}'.", assemblyPath, references[assembly.GetName().FullName]);
          }
        }
      }

      foreach (string assemblyPath in references.Values)
      {
        if (assemblyPath.StartsWith(extensionsFolder, StringComparison.OrdinalIgnoreCase))
        {
          options.AdditionalReferencePaths.Add(assemblyPath);
        }
      }

      builder.Logger().LogInformation("Added ({count}) Razor runtime compliation additional reference paths '{paths}'.", options.AdditionalReferencePaths.Count,
        String.Join(", ", options.AdditionalReferencePaths.Select(path => System.IO.Path.GetRelativePath(Environment.CurrentDirectory, path))));
    });

    return builder;
  }

  /// <summary>
  /// Iterate through assemblies in /bin and /extensions/**, add assemblies which contain Controller classes to Part Manager so that DI/MVC 
  /// can use them. 
  /// </summary>
  /// <param name="builder">IMvcBuilder instance used to configure services.</param>
  /// <remarks>
  /// The call to GetAssembliesImplementing checks for classes which implement Microsoft.AspNetCore.Mvc.Controller.	 
  /// </remarks>
  /// <returns>IMvcBuilder instance.</returns>
  private static IMvcBuilder AddExternalControllers(this IMvcBuilder builder)
  {
    List<string> logEntries = [];

    foreach (Assembly assembly in AssemblyLoader.GetAssembliesImplementing<Controller>(builder.Logger()))
    {
      logEntries.Add($"{assembly.FullName} [{System.IO.Path.GetRelativePath(Environment.CurrentDirectory, assembly.Location)}]");

      AssemblyPart part = new(assembly);

      // only add assembly to application parts if it is not already present (adding twice throws a AmbiguousMatchException)
      if (!ApplicationPartContains(builder, part))
      {
        builder.PartManager.ApplicationParts.Add(part);
      }
    }

    builder.Logger()?.LogInformation("Added ({count}) controllers from: {assemblies}.", logEntries.Count,
      String.Join(", ", logEntries));

    return builder;
  }

  /// <summary>
  /// Add types in assemblies from /bin and in extension folders which implement <see cref="Nucleus.Abstractions.IScheduledTask"/> 
  /// to dependency injection.
  /// </summary>
  /// <param name="builder"></param>
  /// <returns></returns>
  /// <remarks>
  /// Scheduled tasks are added as Transient so that a new instance is created each time the task is run.  This means
  /// that developers of scheduled tasks don't need to worry about making their implementations stateless.
  /// </remarks>
  private static IMvcBuilder AddScheduledTasks(this IMvcBuilder builder)
  {
    List<string> LogEntries = new();

    foreach (Type type in AssemblyLoader.GetTypes<Nucleus.Abstractions.IScheduledTask>())
    {
      LogEntries.Add($"{type.FullName} from {type.Assembly.FullName} [{System.IO.Path.GetRelativePath(Environment.CurrentDirectory, type.Assembly.Location)}]");
      builder.Services.AddTransient(type);
    }

    builder.Logger()?.LogInformation("Added ({count}) scheduled task types: {assemblies}.", LogEntries.Count,
      String.Join(", ", LogEntries));

    return builder;
  }

  /// <summary>
  /// Add types in assemblies from /bin and in extension folders which implement <see cref="Nucleus.Abstractions.Portable.IPortable"/> 
  /// to dependency injection.
  /// </summary>
  /// <param name="builder"></param>
  /// <returns></returns>
  /// <remarks>
  /// IPortable implementations are added as Singleton and must not maintain state.  
  /// </remarks>
  private static IMvcBuilder AddPortable(this IMvcBuilder builder)
  {
    List<string> logEntries = new();

    foreach (Type type in AssemblyLoader.GetTypes<Nucleus.Abstractions.Portable.IPortable>())
    {
      logEntries.Add($"{type.FullName} from {type.Assembly.FullName} [{System.IO.Path.GetRelativePath(Environment.CurrentDirectory, type.Assembly.Location)}]");
      builder.Services.AddSingleton(typeof(Nucleus.Abstractions.Portable.IPortable), type);
    }

    builder.Logger()?.LogInformation("Added ({count}) IPortable implementations: {assemblies}.", logEntries.Count,
      String.Join(", ", logEntries));

    return builder;
  }

  /// <summary>
  /// Check whether an application part is already present in the Part Manager, as adding the same part twice causes 
  /// a runtime AmbiguousMatchException.
  /// </summary>
  /// <param name="builder">IMvcBuilder instance used to configure services.</param>
  /// <param name="part">Part to search for.</param>
  /// <returns></returns>
  private static Boolean ApplicationPartContains(this IMvcBuilder builder, ApplicationPart part)
  {
    return builder.PartManager.ApplicationParts.Contains(part);
    //return builder.PartManager.ApplicationParts.Where(existing => existing is AssemblyPart && existing.Name == part.Name).Any();
  }


  public static IApplicationBuilder UseCompiledRazorResources(this IApplicationBuilder app, IWebHostEnvironment env)
  {
    List<IFileProvider> providers = [];

    foreach (Assembly assembly in AssemblyLoader.ListAssemblies())
    {
      if (assembly.GetManifestResourceStream("Microsoft.Extensions.FileProviders.Embedded.Manifest.xml") != null)
      {
        // test use
        //System.IO.Stream manifest = assembly.GetManifestResourceStream("Microsoft.Extensions.FileProviders.Embedded.Manifest.xml");
        //System.IO.StreamReader reader = new(manifest);
        //string contents = reader.ReadToEnd();

        string requestPath = null;
        IFileProvider provider = null;

        // For control panel implementations, the root path for resources is specified in the ControlPanelAttribute.ResourcesRootPath. 
        Nucleus.Abstractions.ControlPanelAttribute controlPanelAttr = assembly.GetCustomAttribute<Nucleus.Abstractions.ControlPanelAttribute>();
        if (controlPanelAttr != null)
        {
          // use null for "/", because StaticFileOptions throws an exception if we try to set RequestPath to "/" (even though "/" is the 
          // default value)
          if (controlPanelAttr.ResourcesRootPath != "/")
          {
            requestPath = controlPanelAttr.ResourcesRootPath;
          }
          provider = new ManifestEmbeddedFileProvider(assembly, "/");
        }
        else
        {
          // For extensions, we derive the root path for resources using the extension name.  The derived root path is 
          // "/Extensions/[extension-name]".  
          foreach (System.Type extensionType in AssemblyLoader.GetTypesWithAttribute<Nucleus.Abstractions.ExtensionAttribute>(assembly))
          {
            // requestPath for extensions is null because the Extensions/extension-name prefix is handled by the ExtensionManifestEmbeddedFileProvider.
            Nucleus.Abstractions.ExtensionAttribute extensionAttr = extensionType.GetCustomAttribute<Nucleus.Abstractions.ExtensionAttribute>();
            if (extensionAttr != null)
            {
              provider = new Nucleus.Core.FileProviders.NucleusExtensionManifestEmbeddedFileProvider(assembly, extensionAttr.RouteValue);
              break;
            }
          }
        }
        if (provider == null)
        {
          // the assembly is not a control panel implementation or an extension (but contains embedded files)
          provider = new ManifestEmbeddedFileProvider(assembly, "/");
        }

        app.UseStaticFiles(new StaticFileOptions
        {
          FileProvider = provider,
          RequestPath = requestPath,
          OnPrepareResponse = context =>
          {
            // Add charset=utf-8 to content-type for text content if it is not already present
            if ((context.Context.Response.ContentType.StartsWith("text/") || context.Context.Response.ContentType.StartsWith("application/javascript")) && !context.Context.Response.ContentType.Contains("utf-8", StringComparison.OrdinalIgnoreCase))
            {
              context.Context.Response.ContentType += "; charset=utf-8";
            }

            // Cache static content for 30 days
            context.Context.Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
            {
              Public = true,
              MaxAge = TimeSpan.FromDays(30)
            };
          }
        });

        providers.Add(provider);
        
      }
    }

    if (env.ContentRootFileProvider is CompositeFileProvider compositeFileProvider)
    {
      providers.InsertRange(0, compositeFileProvider.FileProviders);
    }
    else
    {
      providers.Insert(0, env.ContentRootFileProvider);
    }

    env.ContentRootFileProvider = new CompositeFileProvider(providers);

    return app;
  }

  /// <summary>
  /// Loop through assemblies, look for compiled Razor assemblies, and add to Part Manager so that Mvc can use them
  /// </summary>
  /// <param name="builder">IMvcBuilder instance used to configure services.</param>
  /// <returns></returns>
  /// <remarks>
  /// Application parts are also configured in AddExternalControllers(), so in many (most) cases, the PartManager will already
  /// contain the ApplicationPart for the assembly which contains compiled Razor views, so we check to ensure that we do not 
  /// add a part twice by checking for it in the call to ApplicationPartContains().
  /// </remarks>
  private static IMvcBuilder AddCompiledRazorViews(this IMvcBuilder builder)
  {
    List<string> logEntries = new();

    foreach (Assembly assembly in AssemblyLoader.GetAssembliesWithAttribute<Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute>())
    {
      foreach (ApplicationPart part in CompiledRazorAssemblyApplicationPartFactory.GetDefaultApplicationParts(assembly))
      {
        if (!ApplicationPartContains(builder, part))
        {
          logEntries.Add(part.Name);
          builder.PartManager.ApplicationParts.Add(part);
        }
      }

      builder.Logger()?.LogInformation("Added ({count}) compiled Razor Views from: {assemblies}].", logEntries.Count,
        String.Join(", ", logEntries));
    }

    return builder;
  }

  /// <summary>
  /// Returns a list of assemblies which implement IHostingStartup.
  /// </summary>
  /// <param name="path">The content root path.</param>
  public static IEnumerable<string> GetHostingStartupAssemblies(ILogger logger)
  {
    return AssemblyLoader.GetAssembliesImplementing<Microsoft.AspNetCore.Hosting.IHostingStartup>(logger)
      .Select(assembly => assembly.GetName().FullName);
  }
}
