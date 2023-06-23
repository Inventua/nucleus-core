using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Data.EntityFramework;
using Nucleus.DNN.Migration.DataProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: HostingStartup(typeof(Nucleus.DNN.Migration.Startup))]

namespace Nucleus.DNN.Migration;

public class Startup : IHostingStartup
{
  public const string DNN_SCHEMA_NAME = "DNN";

  public void Configure(IWebHostBuilder builder)
  {
    builder.ConfigureServices((context, services) =>
    {
      services.AddSingleton<DNNMigrationManager>();
      services.AddDataProvider<DNNMigrationDataProvider, DNNMigrationDataProvider, DNNMigrationDbContext>(context.Configuration);
      try
      {
        services.AddDataProvider<DNNDataProvider, DNNDataProvider, DNNDbContext>(context.Configuration, DNN_SCHEMA_NAME);
      }
      catch(InvalidOperationException) 
      {
        // suppress.  This happens when there is no database configuration schema with name="DNN".  The GetDNNVersion function return NULL
        // when there is no DNNDataProvider registered, and the main view shows a warning message.
      }
    });
  }
}
