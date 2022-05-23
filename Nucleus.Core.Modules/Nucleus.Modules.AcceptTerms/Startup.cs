using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Data.EntityFramework;
using Nucleus.Modules.AcceptTerms.DataProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: HostingStartup(typeof(Nucleus.Modules.AcceptTerms.Startup))]

namespace Nucleus.Modules.AcceptTerms
{
  public class Startup : IHostingStartup
  {
    public void Configure(IWebHostBuilder builder)
    {
      builder.ConfigureServices((context, services) =>
      {
        services.AddSingleton<AcceptTermsManager>();
        services.AddDataProvider<IAcceptTermsDataProvider, DataProviders.AcceptTermsDataProvider, DataProviders.AcceptTermsDbContext>(context.Configuration);
      });
    }
  }
}
