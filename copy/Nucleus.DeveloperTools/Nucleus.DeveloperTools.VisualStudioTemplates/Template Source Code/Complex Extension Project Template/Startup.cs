using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using $nucleus_extension_namespace$.DataProviders;

[assembly:HostingStartup(typeof($nucleus_extension_namespace$.Startup))]

namespace $nucleus_extension_namespace$
{

	public class Startup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) => 
			{
				services.AddSingleton<$nucleus_extension_name$Manager > ();
				services.AddTransient<I$nucleus_extension_name$DataProvider, DataProviders.SQLite.SQLiteDataProvider>();
			});
		}
	}
}
