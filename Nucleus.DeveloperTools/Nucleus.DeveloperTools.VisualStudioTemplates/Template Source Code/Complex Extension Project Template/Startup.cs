using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Data.EntityFramework;
using $nucleus.extension.namespace$.DataProviders;

[assembly:HostingStartup(typeof($nucleus.extension.namespace$.Startup))]

namespace $nucleus.extension.namespace$
{
	public class Startup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) => 
			{
				services.AddSingleton<$nucleus.extension.name$Manager>();
				services.AddDataProvider<I$nucleus.extension.name$DataProvider, DataProviders.$nucleus.extension.name$DataProvider, DataProviders.$nucleus.extension.name$DbContext > (context.Configuration);
			});
		}
	}
}
