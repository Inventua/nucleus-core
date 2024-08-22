using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Search;

[assembly: HostingStartup(typeof(Nucleus.Extensions.GoogleCustomSearch.Startup))]

namespace Nucleus.Extensions.GoogleCustomSearch;

public class Startup : IHostingStartup
{
  public void Configure(IWebHostBuilder builder)
  {
    builder.ConfigureServices((context, services) =>
    {
      services.AddTransient<ISearchProvider, SearchProvider>();
    });
  }
}
