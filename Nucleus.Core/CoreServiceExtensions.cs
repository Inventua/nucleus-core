using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Nucleus.Core.Layout;
using Nucleus.Core.EventHandlers;
using Nucleus.Core.FileSystemProviders;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Mail;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.Search;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Core.Mail;
using Nucleus.Abstractions.Models;
using Nucleus.Core.Plugins;
using Nucleus.Abstractions;
using System.Reflection;
using System.Configuration;
using Microsoft.AspNetCore.Builder;

namespace Nucleus.Core;

public static class CoreServiceExtensions
{

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
    services.AddScoped<SecurityHeadersMiddleware>();
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
    services.AddScoped<DefaultNoCacheMiddleware>();
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
  /// Use the specified Nucleus control panel.
  /// </summary>
  /// <remarks>
  /// If the name is an empty string, the admin UI is not rendered. If the specified control panel name is not found, the default control panel is used.
  /// </remarks>
  /// <param name="app"></param>
  /// <param name="name"></param>
  public static void UseControlPanel(this IApplicationBuilder app, string name)
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

      if (panels.Count() == 0)
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
  }

  public class ConfigureStoreOptions : IPostConfigureOptions<StoreOptions>
  {
    public void PostConfigure(string name, StoreOptions options)
    {
      // Add default value if config contains no values
      if (!options.Stores.Any())
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

        store.APIPath = store.APIPath.TrimStart(new char[] { '/' });
        store.ViewerPath = store.ViewerPath.TrimStart(new char[] { '/' });
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

