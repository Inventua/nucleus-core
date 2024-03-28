using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Modules.PageLinks.DataProviders;
using Nucleus.Data.EntityFramework;
using Nucleus.Data.Common;

[assembly: HostingStartup(typeof(Nucleus.Modules.PageLinks.Startup))]

namespace Nucleus.Modules.PageLinks
{
	public class Startup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) =>
			{
        services.AddSingleton<PageLinksManager>();
        services.AddDataProvider<IPageLinksDataProvider, DataProviders.PageLinksDataProvider, DataProviders.PageLinksDbContext>(context.Configuration);
      });
		}
	}
}
