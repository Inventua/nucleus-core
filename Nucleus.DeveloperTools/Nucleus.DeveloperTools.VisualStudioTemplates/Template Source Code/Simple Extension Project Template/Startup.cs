using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly:HostingStartup(typeof($nucleus_extension_namespace$.Startup))]

namespace $nucleus_extension_namespace$
{

	public class Startup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) => 
			{
			});
		}
	}
}
