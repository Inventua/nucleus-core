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

namespace Nucleus.Core
{
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
		/// Add and configure Nucleus folder options
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configuration"></param>
		/// <returns></returns>
		public static IServiceCollection AddFolderOptions(this IServiceCollection services, IConfiguration configuration)
		{
			// Register action & post-configuration for folder options (as normal)
			AddOption<FolderOptions>(services, configuration, FolderOptions.Section);
			services.ConfigureOptions(typeof(ConfigureFolderOptions));

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
			services.AddSingleton<IEventDispatcher, Services.EventDispatcher>();
			services.AddTransient<IMailClientFactory, Mail.MailClientFactory>(); 
			
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

			// Search
			services.AddTransient<IContentMetaDataProducer, Search.PageMetaDataProducer>();
			services.AddTransient<IContentMetaDataProducer, Search.FileMetaDataProducer>();

			// Built-in Event handlers
			services.AddCoreEventHandlers();

			// Built-in file system providers
			services.AddFileSystemProviders(configuration);			

			// Scheduler
			services.AddSingleton<Abstractions.Models.TaskScheduler.RunningTaskQueue>();
			services.AddHostedService<Services.TaskScheduler>();

			// config options
			AddOption<PasswordOptions>(services, configuration, PasswordOptions.Section);
			AddOption<ClaimTypeOptions>(services, configuration, ClaimTypeOptions.Section);			
			AddOption<SmtpMailOptions>(services, configuration, SmtpMailOptions.Section);
			AddOption<HtmlEditorOptions>(services, configuration, HtmlEditorOptions.Section);
			
			return services;
		}

		public static void AddOption<T>(IServiceCollection services, IConfiguration configuration, string key) where T : class, new()
		{
			services.Configure<T>(configuration.GetSection(key), binderOptions => binderOptions.BindNonPublicProperties = true); 
		}


		public class ConfigureFolderOptions : IPostConfigureOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions>
		{			
			public ConfigureFolderOptions()
			{
				
			}

			public void PostConfigure(string name, Nucleus.Abstractions.Models.Configuration.FolderOptions options)
			{
				PostConfigure(options);
			}

			public static void PostConfigure(Nucleus.Abstractions.Models.Configuration.FolderOptions options)
			{
				if (String.IsNullOrEmpty(options.DataFolder))
				{
					options.DataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Nucleus");
				}

				if (!System.IO.Directory.Exists(options.DataFolder))
				{
					System.IO.Directory.CreateDirectory(options.DataFolder);
				}
			}
		}
	}
}

