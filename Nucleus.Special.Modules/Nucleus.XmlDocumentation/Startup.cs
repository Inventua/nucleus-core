using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// XML Serializer doesn't work with assembly load contexts
// https://github.com/dotnet/runtime/issues/1388
// https://github.com/dotnet/runtime/pull/58932

[assembly: HostingStartup(typeof(Nucleus.XmlDocumentation.Startup))]

namespace Nucleus.XmlDocumentation
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
