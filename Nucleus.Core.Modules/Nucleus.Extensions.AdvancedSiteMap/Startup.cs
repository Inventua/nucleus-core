using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Search;
using Microsoft.AspNetCore.Builder;

[assembly: HostingStartup(typeof(Nucleus.Extensions.AdvancedSiteMap.Startup))]

namespace Nucleus.Extensions.AdvancedSiteMap
{
	public class Startup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) =>
			{
				services.AddTransient<ISearchIndexManager, SearchIndexManager>();				
			});						
		}
	}
}
