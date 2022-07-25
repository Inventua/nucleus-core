using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;

[assembly:HostingStartup(typeof(Nucleus.Modules.StaticContent.Startup))]

namespace Nucleus.Modules.StaticContent
{

	public class Startup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) => 
			{
				services.AddScoped<Nucleus.Abstractions.EventHandlers.ISystemEventHandler<Nucleus.Abstractions.Models.FileSystem.File, Nucleus.Abstractions.EventHandlers.SystemEventTypes.Update>, FileEventHandler>();
			});
		}
	}

	public class FileEventHandler : Nucleus.Abstractions.EventHandlers.ISystemEventHandler<Nucleus.Abstractions.Models.FileSystem.File, Nucleus.Abstractions.EventHandlers.SystemEventTypes.Update>
	{
		private ICacheManager CacheManager { get; }
		private IExtensionManager ExtensionManager { get; }
		private Context Context { get; }

		public FileEventHandler(Context context, ICacheManager cacheManager, IExtensionManager extensionManager)
		{
			this.Context = context;
			this.CacheManager = cacheManager;
			this.ExtensionManager = extensionManager;
		}

		public async Task Invoke(Nucleus.Abstractions.Models.FileSystem.File file)
		{
			// if a file changes, clear the static content cache for static content modules which use the specified file

			// This must match the value in package.xml
			Guid moduleDefinitionId = Guid.Parse("0930d4fe-0469-47e6-a28b-7c42d85a61fd");

			foreach (PageModule module in await this.ExtensionManager.ListPageModules(new Nucleus.Abstractions.Models.ModuleDefinition() { Id = moduleDefinitionId }))
			{
				Models.Settings settings = new();

				settings.ReadSettings(module);

				if (settings.DefaultFileId == file.Id)
				{
					this.CacheManager.StaticContentCache().Remove(this.Context.Site.Id + ":" + file.Id);
				}
			}
		}
	}
}
