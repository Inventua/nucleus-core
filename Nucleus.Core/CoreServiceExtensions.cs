using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Conversion;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.Mail;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Abstractions.Search;
using Nucleus.Core.EventHandlers;
using Nucleus.Core.FileSystemProviders;
using Nucleus.Core.Layout;
using Nucleus.Core.Logging;
using Nucleus.Core.Plugins;
using Nucleus.Core.Services.Conversion;

namespace Nucleus.Core;

public static class CoreServiceExtensions
{
  /// <summary>
  /// Cache settings for static files.
  /// </summary>
  internal static Microsoft.Net.Http.Headers.CacheControlHeaderValue STATIC_FILES_CACHE_CONTROL = new()
  {
    Public = true,
    MaxAge = TimeSpan.FromDays(30)
  };

  /// <summary>
  /// Add and configure security headers middleware.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <returns></returns>
  public static IServiceCollection AddSecurityHeadersMiddleware(this IServiceCollection services, IConfiguration configuration)
  {
    // Register action & post-configuration for security header options 
    AddOption<SecurityHeaderOptions>(services, configuration, SecurityHeaderOptions.Section);
    // Add middleware
    services.AddSingleton<SecurityHeadersMiddleware>();
    return services;
  }

  /// <summary>
  /// Add content converters.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <returns></returns>
  public static IServiceCollection AddConverters(this IServiceCollection services)
  {
    services.AddSingleton<Nucleus.Abstractions.Conversion.IContentConverter, BasicContentConverter>();
    services.AddSingleton<IContentConverterFactory, ContentConverterFactory>();
    return services;
  }

  /// <summary>
  /// Add and configure default cache middleware.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <returns></returns>
  public static IServiceCollection AddDefaultCacheMiddleware(this IServiceCollection services, IConfiguration configuration)
  {
    // Add middleware
    services.AddSingleton<DefaultNoCacheMiddleware>();
    return services;
  }

  /// <summary>
  /// Add and configure Nucleus folder options
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <returns></returns>
  public static IServiceCollection AddFolderOptions(this IServiceCollection services, IConfiguration configuration)
  {
    // Register action & post-configuration for folder options (as normal)
    AddOption<FolderOptions>(services, configuration, FolderOptions.Section);
    services.ConfigureOptions<ConfigureFolderOptions>();

    return services;
  }

  /// <summary>
  /// Add core service "manager" classes to the dependency injection services collection
  /// </summary>
  /// <param name="services"></param>
  /// <returns></returns>
  public static IServiceCollection AddNucleusCoreServices(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddNucleusLayout();

    // General-use services 
    services.AddSingleton<Nucleus.Abstractions.Models.Application>();

    services.AddSingleton<IEventDispatcher, Services.EventDispatcher>();
    services.AddTransient<IMailClientFactory, Mail.MailClientFactory>();
    services.AddTransient<Abstractions.IPreflight, Nucleus.Core.Services.Preflight>();
    services.AddSingleton<Abstractions.IRestApiClient, Services.RestApiClient>();
    services.AddSingleton<ICacheManager, Managers.CacheManager>();

    // Extension managers
    services.AddSingleton<ILayoutManager, Managers.LayoutManager>();
    services.AddSingleton<IContainerManager, Managers.ContainerManager>();
    services.AddSingleton<IExtensionManager, Managers.ExtensionManager>();

    // Model managers (services)
    services.AddSingleton<ISiteGroupManager, Managers.SiteGroupManager>();
    services.AddSingleton<ISiteManager, Managers.SiteManager>();
    services.AddSingleton<IPageManager, Managers.PageManager>();
    services.AddSingleton<IPageModuleManager, Managers.PageModuleManager>();
    services.AddSingleton<IUserManager, Managers.UserManager>();

    services.AddSingleton<IRoleManager, Managers.RoleManager>();
    services.AddSingleton<IRoleGroupManager, Managers.RoleGroupManager>();
    services.AddSingleton<IMailTemplateManager, Managers.MailTemplateManager>();
    services.AddSingleton<ISessionManager, Managers.SessionManager>();

    services.AddSingleton<IScheduledTaskManager, Managers.ScheduledTaskManager>();
    services.AddSingleton<IListManager, Managers.ListManager>();

    services.AddSingleton<IFileSystemManager, Managers.FileSystemManager>();
    services.AddSingleton<IContentManager, Managers.ContentManager>();
    services.AddSingleton<IPermissionsManager, Managers.PermissionsManager>();

    services.AddSingleton<IApiKeyManager, Managers.ApiKeyManager>();
    services.AddSingleton<IOrganizationManager, Managers.OrganizationManager>();
    services.AddSingleton<IExtensionsStoreManager, Managers.ExtensionsStoreManager>();

    services.AddSingleton<ISearchIndexHistoryManager, Search.SearchIndexHistoryManager>();

    // Search
    services.AddTransient<IContentMetaDataProducer, Search.PageMetaDataProducer>();
    services.AddTransient<IContentMetaDataProducer, Search.FileMetaDataProducer>();

    // Built-in Event handlers
    services.AddCoreEventHandlers();

    // Built-in file system providers
    services.AddFileSystemProviders(configuration);

    // Scheduler
    services.AddSingleton<Abstractions.Models.TaskScheduler.RunningTaskQueue>();

    // We add the task scheduler separately as a singleton so that we can use the same instance when also adding it as both a 
    // IHostedService and ISystemEventHandler.  The Task scheduler implements ISystemEventHandler in order to  immediately 
    // re-evaluate a scheduled task after it has been updated/changed.
    services.AddSingleton<Services.TaskScheduler>();
    services.AddHostedService<Services.TaskScheduler>(serviceProvider => serviceProvider.GetRequiredService<Services.TaskScheduler>());
    services.AddSingleton<ISingletonSystemEventHandler<ScheduledTask, Nucleus.Abstractions.EventHandlers.SystemEventTypes.Update>>(serviceProvider => serviceProvider.GetRequiredService<Services.TaskScheduler>());

    // config options
    services.AddOption<ResourceFileOptions>(configuration, ResourceFileOptions.Section);
    services.AddOption<PasswordOptions>(configuration, PasswordOptions.Section);
    services.AddOption<ClaimTypeOptions>(configuration, ClaimTypeOptions.Section);
    services.AddOption<HtmlEditorOptions>(configuration, HtmlEditorOptions.Section);
    services.AddOption<StoreOptions>(configuration, StoreOptions.Section);

    services.ConfigureOptions<ConfigureStoreOptions>();

    return services;
  }

  public static void AddOption<T>(this IServiceCollection services, IConfiguration configuration, string key) where T : class, new()
  {
    services.Configure<T>(configuration.GetSection(key), binderOptions => binderOptions.BindNonPublicProperties = true);
  }

  /// <summary>
  /// Adds a configuration provider which replaces environment variables in configuration values with environment variable values. The
  /// environment variable token is expressed in the form: %ENVIRONMENT_VARIABLE_NAME%. This method must be called after adding all other
  /// configuration providers, so that the configuration values that it contributes take precedence over all others.
  /// </summary>
  /// <param name="builder"></param>
  /// <returns></returns>
  public static IConfigurationBuilder ExpandEnvironmentVariables(this IConfigurationBuilder builder)
  {
    // Load values from previously added configuration sources, replace tokens which represent environment variables with environment
    // variable vaues. This operation must take place before adding the ExpandEnvironmentVariablesConfigurationSource, otherwise it 
    // causes infinite recursion & a stack overflow.
    Dictionary<string, string> settings = [];

    // build configuration settings so we can iterate through them. This has the downside of calling build.Build twice (once here, and
    // once when the "real" call to populate configuration classes takes place), but has the benefit of working automatically with *all*
    // settings.
    IEnumerable<KeyValuePair<string, string>> configurationValues = builder.Build().AsEnumerable().ToList();

    // iterate through all configuration settings, look for tokens which represent environment variables (which match the
    // pattern %env-variable-name%) and replace the tokens with the environment variable value(s).
    foreach (KeyValuePair<string, string> keyValue in configurationValues)
    {
      if (!String.IsNullOrEmpty(keyValue.Value))
      {
        string newValue = Environment.ExpandEnvironmentVariables(keyValue.Value);
        
        // if the value has been changed, add it 
        if (newValue != keyValue.Value)
        {
          settings.Add(keyValue.Key, newValue);
        }
      }
    }

    builder.Add(new ExpandEnvironmentVariablesConfigurationSource(settings));

    return builder;
  }

  /// <summary>
  /// Add Blazor and Razor components support (server and webassembly)
  /// </summary>
  /// <param name="services"></param>
  /// <returns></returns>
  public static IServiceCollection AddBlazor(this IServiceCollection services)
  {
    services.AddRazorComponents()
      .AddInteractiveServerComponents()
      .AddInteractiveWebAssemblyComponents();
    //AddHubOptions()

    // Experimental.  Accompanying code (commented out) is Nucleus.Extensions\BlazorExtensions.cs
    // add IActionResultExecutor for Nucleus.Extensions.BlazorExtensions.ComponentView
    // services.AddScoped<IActionResultExecutor<ComponentViewResult>, ComponentViewResultExecutor>();

    return services;
  }

  public static IEndpointRouteBuilder UseBlazor<T>(this IApplicationBuilder app, IWebHostEnvironment env, IEndpointRouteBuilder routes)
  {
    if (env.IsDevelopment())
    {
      app.UseWebAssemblyDebugging();

      // Disable WebAssembly "hot reload", because it doesn't work with dynamically loaded Web Assemblies which are part of a Nucleus extension.  Any value that is
      // not "debug" will disable wasm "hot reload".  This only affects Nucleus when debugging.

      // Code reference: https://github.com/dotnet/aspnetcore/blob/main/src/Components/WebAssembly/Server/src/ComponentsWebAssemblyApplicationBuilderExtensions.cs
      System.Environment.SetEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES", "Release");
    }

    // map razor/blazor components, add server render modes and webassembly render modes, add assemblies in the extensions folder/subfolders which
    // contain razor/blazor components
    routes.MapRazorComponents<T>()
      .AddInteractiveServerRenderMode()
      .AddInteractiveWebAssemblyRenderMode()

      // scan extensions which contain razor/blazor components for pages and map their endpoints.  Razor pages (which define their own routes)
      // are not expected to be a common use case, but there's no reason not to support them.
      .AddAdditionalAssemblies
      (
        AssemblyLoader.GetAssembliesImplementing<Microsoft.AspNetCore.Components.ComponentBase>(app.Logger())
          .Where(assembly => assembly.Location.StartsWith(FolderOptions.GetExtensionsFolderStatic(false)))
          .ToArray()
      );

    return routes;
  }

  /// <summary>
  /// Add static file paths for the extensions, resources, shared and areas folders.
  /// </summary>
  /// <param name="app"></param>
  /// <param name="env"></param>
  /// <returns></returns>
  public static IApplicationBuilder UseStaticFilePaths(this IApplicationBuilder app, IWebHostEnvironment env)
  {
    List<IFileProvider> providers = [];

    foreach (string folderName in Nucleus.Abstractions.Models.Configuration.FolderOptions.ALLOWED_STATICFILE_PATHS)
    {
      string path = Nucleus.Abstractions.Models.Configuration.FolderOptions.NormalizePath(System.IO.Path.Combine(env.ContentRootPath, folderName));

      if (System.IO.Directory.Exists(path))
      {
        app.Logger()?.LogInformation("Adding static file path: [{path}]", "/" + folderName);
        IFileProvider fileProvider = new PhysicalFileProvider(path);
        
        app.UseStaticFiles(new StaticFileOptions
        {
          FileProvider = fileProvider,
          RequestPath = "/" + folderName,
          OnPrepareResponse = context =>
          {
            // Add charset=utf-8 to content-type for text content if it is not already present
            if ((context.Context.Response.ContentType.StartsWith("text/") || context.Context.Response.ContentType.StartsWith("application/javascript")) && !context.Context.Response.ContentType.Contains("utf-8", StringComparison.OrdinalIgnoreCase))
            {
              context.Context.Response.ContentType += "; charset=utf-8";
            }

            // Cache static content for 30 days
            context.Context.Response.GetTypedHeaders().CacheControl = STATIC_FILES_CACHE_CONTROL;
          }
        });

        providers.Add(fileProvider);
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
  /// Use the specified Nucleus control panel.
  /// </summary>
  /// <remarks>
  /// If the name is an empty string, the admin UI is not rendered. If the specified control panel name is not found, the default control panel is used.
  /// </remarks>
  /// <param name="app"></param>
  /// <param name="name"></param>
  public static IApplicationBuilder UseControlPanel(this IApplicationBuilder app, string name)
  {
    Application appData = app.ApplicationServices.GetService<Application>();

    if (name == "")
    {
      appData.SetControlPanelUri("");
    }
    else
    {
      IEnumerable<ControlPanelAttribute> panels = AssemblyLoader.GetAssembliesWithAttribute<ControlPanelAttribute>()
        .Select(assembly => assembly.GetCustomAttribute<ControlPanelAttribute>());

      if (!panels.Any())
      {
        // there are no control panel implementations
        appData.SetControlPanelUri("");
      }
      else if (panels.Count() == 1)
      {
        // use the one available control panel
        appData.SetControlPanelUri(panels.First().Uri);
      }
      else
      {
        // more than one control panel is available. Use the selected control panel if found, or use the default if not found.    
        appData.SetControlPanelUri(panels
          .Where(panel => !String.IsNullOrEmpty(name) && panel.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
          .FirstOrDefault()
          ?.Uri);
      }
    }

    return app;
  }

  public class ConfigureStoreOptions : IPostConfigureOptions<StoreOptions>
  {
    public void PostConfigure(string name, StoreOptions options)
    {
      // Add default value if config contains no values
      if (options.Stores.Count == 0)
      {
        options.Stores.Add(Store.Default);
      }

      // ensure that baseuri has a trailing slash, remove leading slashes from relative paths, ensure relative paths end with a slash.  This 
      // is required because the System.Uri constructors 
      foreach (Store store in options.Stores)
      {
        if (!store.BaseUrl.EndsWith('/'))
        {
          store.BaseUrl += "/";
        }

        if (!store.APIPath.EndsWith('/'))
        {
          store.APIPath += "/";
        }

        if (!store.ViewerPath.EndsWith('/'))
        {
          store.ViewerPath += "/";
        }

        store.APIPath = store.APIPath.TrimStart(['/']);
        store.ViewerPath = store.ViewerPath.TrimStart(['/']);
      }
    }
  }

  public class ConfigureFolderOptions : IPostConfigureOptions<FolderOptions>
  {
    public ConfigureFolderOptions()
    {

    }

    public void PostConfigure(string name, FolderOptions options)
    {
      try
      {
        options.SetDefaultDataFolder(true);
      }
      catch (System.UnauthorizedAccessException)
      {
        // file permissions error.  Ignore so that the install wizard can report the permissions error when it 
        // does config checking.
      }
    }
  }
}

