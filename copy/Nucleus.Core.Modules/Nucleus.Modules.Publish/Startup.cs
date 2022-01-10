using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Modules.Publish.DataProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                services.AddTransient<IArticleDataProvider, DataProviders.SQLite.SQLiteDataProvider>();
            });
        }
    }
}
