using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Modules.Forums.DataProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: HostingStartup(typeof(Nucleus.Modules.Forums.Startup))]

namespace Nucleus.Modules.Forums
{
	public class Startup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) =>
			{
				services.AddSingleton<GroupsManager>();
				services.AddSingleton<ForumsManager>();
				services.AddTransient<IForumsDataProvider, DataProviders.SQLite.SQLiteDataProvider>();
			});
		}
	}
}
