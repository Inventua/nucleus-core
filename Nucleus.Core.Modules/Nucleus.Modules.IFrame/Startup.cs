using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: HostingStartup(typeof(Nucleus.Modules.IFrame.Startup))]

namespace Nucleus.Modules.IFrame;
public class Startup : IHostingStartup
{
  public void Configure(IWebHostBuilder builder)
  {
    builder.ConfigureServices((context, services) =>
    {
    });
  }
}
