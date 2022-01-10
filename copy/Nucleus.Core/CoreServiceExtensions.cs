using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Core.Layout;
using Nucleus.Core.EventHandlers;
using Nucleus.Core.FileSystemProviders;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Logging;
using Nucleus.Core.Logging;

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
			services.AddSingleton<EventDispatcher>();
			services.AddScoped<MailClientFactory>(); 
			
			services.AddSingleton<CacheManager>();
			
			// Extension managers
			services.AddSingleton<LayoutManager>();
			services.AddSingleton<ContainerManager>();
			services.AddSingleton<ExtensionManager>();

			// Model managers (services)
			services.AddSingleton<SiteGroupManager>();
			services.AddSingleton<SiteManager>();
			services.AddSingleton<PageManager>();
			services.AddSingleton<PageModuleManager>();
			services.AddSingleton<UserManager>();

			services.AddSingleton<RoleManager>();
			services.AddSingleton<RoleGroupManager>();
			services.AddSingleton<MailTemplateManager>();
			services.AddSingleton<SessionManager>();

			services.AddSingleton<ScheduledTaskManager>();
			services.AddSingleton<ListManager>();

			services.AddSingleton<FileSystemManager>();

			// Built-in Event handlers
			services.AddCoreEventHandlers();

			// Built-in file system providers
			services.AddFileSystemProviders(configuration);			

			// Scheduler
			services.AddSingleton<Abstractions.Models.TaskScheduler.RunningTaskQueue>();
			services.AddHostedService<TaskScheduler>();

			// config options
			AddOption<PasswordOptions>(services, configuration, PasswordOptions.Section);
			AddOption<ClaimTypeOptions>(services, configuration, ClaimTypeOptions.Section);
			AddOption<CacheOptions>(services, configuration, CacheOptions.Section);
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

