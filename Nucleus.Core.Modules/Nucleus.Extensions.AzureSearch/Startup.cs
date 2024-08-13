using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Search;

[assembly: HostingStartup(typeof(Nucleus.Extensions.AzureSearch.Startup))]

namespace Nucleus.Extensions.AzureSearch;
public class Startup : IHostingStartup
{
  public void Configure(IWebHostBuilder builder)
  {
    builder.ConfigureServices((context, services) =>
    {
      services.AddTransient<ISearchIndexManager, AzureSearchIndexManager>();
      services.AddTransient<ISearchProvider, AzureSearchProvider>();
    });
  }
}
