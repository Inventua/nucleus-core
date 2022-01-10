using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Modules.Links.DataProviders;
using Nucleus.Data.EntityFramework;
using Nucleus.Data.Common;

[assembly: HostingStartup(typeof(Nucleus.Modules.Links.Startup))]

namespace Nucleus.Modules.Links
{
	public class Startup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) =>
			{
				services.AddSingleton<LinksManager>();
				services.AddDataProvider<ILinksDataProvider, DataProviders.LinksDataProvider, DataProviders.LinksDbContext>(context.Configuration);
			});
		}
	}
}
