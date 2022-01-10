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

namespace Nucleus.Core
{
	public static class CoreServiceExtensions
	{	
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
			services.AddScoped<IMailClientFactory, Mail.MailClientFactory>(); 
			
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
			
			// Replaced by code in CacheManager.Add
			//AddOption<CacheOptions>(services, configuration, CacheOptions.Section);
			AddOption<SmtpMailOptions>(services, configuration, SmtpMailOptions.Section);
			AddOption<HtmlEditorOptions>(services, configuration, HtmlEditorOptions.Section);
			
			return services;
		}

		public static void AddOption<T>(IServiceCollection services, IConfiguration configuration, string key) where T : class, new()
		{
			services.Configure<T>(configuration.GetSection(key), binderOptions => binderOptions.BindNonPublicProperties = true); 
		}	
	
	}
}

