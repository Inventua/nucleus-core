using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Conversion;

[assembly: HostingStartup(typeof(Nucleus.Extensions.TikaServerConverter.Startup))]

namespace Nucleus.Extensions.TikaServerConverter;
public class Startup : IHostingStartup
{
  public void Configure(IWebHostBuilder builder)
  {
    builder.ConfigureServices((context, services) =>
    {
      services.AddSingleton<IContentConverter, TikaServerContentConverter>();
    });
  }
}
