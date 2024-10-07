using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Modules.Maps.MapRenderers;

[assembly: HostingStartup(typeof(Nucleus.Modules.Maps.Startup))]

namespace Nucleus.Modules.Maps
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
