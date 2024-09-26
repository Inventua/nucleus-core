using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Search;

[assembly: HostingStartup(typeof(Nucleus.Extensions.TypeSense.Startup))]

namespace Nucleus.Extensions.TypeSense;
public class Startup : IHostingStartup
{
  public void Configure(IWebHostBuilder builder)
  {
    builder.ConfigureServices((context, services) =>
    {
      services.AddTransient<ISearchIndexManager, TypeSenseIndexManager>();
      services.AddTransient<ISearchProvider, TypeSenseSearchProvider>();
    });
  }
}
