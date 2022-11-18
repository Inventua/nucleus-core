using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Data.EntityFramework;
using Nucleus.SAML.Server.DataProviders;

[assembly: HostingStartup(typeof(Nucleus.SAML.Server.Startup))]

namespace Nucleus.SAML.Server
{
	public class Startup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) =>
			{
				services.AddSingleton<ClientAppManager>();
				services.AddSingleton<ClientAppTokenManager>();

				services.AddDataProvider<IClientAppDataProvider, SAMLServerDataProvider, SAMLServerDbContext>(context.Configuration);
				services.AddDataProvider<IClientAppTokenDataProvider, SAMLServerDataProvider, SAMLServerDbContext>(context.Configuration);


			});
		}
	}
}
