using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Data.EntityFramework;
using Nucleus.OAuth.Server.DataProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: HostingStartup(typeof(Nucleus.OAuth.Server.Startup))]

namespace Nucleus.OAuth.Server
{
	public class Startup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) =>
			{
				services.AddSingleton<ClientAppManager>();
				services.AddSingleton<ClientAppTokenManager>();

				services.AddDataProvider<IClientAppDataProvider, DataProviders.OAuthServerDataProvider, DataProviders.OAuthServerDbContext>(context.Configuration);
				services.AddDataProvider<IClientAppTokenDataProvider, DataProviders.OAuthServerDataProvider, DataProviders.OAuthServerDbContext>(context.Configuration);


			});
		}
	}
}
