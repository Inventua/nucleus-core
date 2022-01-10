using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Modules.Links.DataProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                services.AddTransient<ILinksDataProvider, DataProviders.SQLite.SQLiteDataProvider>();
            });
        }
    }
}
