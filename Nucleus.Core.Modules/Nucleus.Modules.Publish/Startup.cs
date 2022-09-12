using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Modules.Publish.DataProviders;
using Nucleus.Data.EntityFramework;
using Nucleus.Abstractions.Search;

[assembly: HostingStartup(typeof(Nucleus.Modules.Publish.Startup))]

namespace Nucleus.Modules.Publish
{
	public class Startup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) =>
			{
				services.AddSingleton<ArticlesManager>();
				services.AddDataProvider<IArticlesDataProvider, DataProviders.ArticlesDataProvider, DataProviders.ArticlesDbContext>(context.Configuration);

				services.AddSingleton<HeadlinesManager>();
				services.AddDataProvider<IHeadlinesDataProvider, DataProviders.HeadlinesDataProvider, DataProviders.HeadlinesDbContext>(context.Configuration);

				services.AddTransient<IContentMetaDataProducer, ArticlesMetaDataProducer>();
			});
		}
	}
}
