## Dependency Injection 
Your extension can add services to dependency injection or perform other startup tasks by implementing the 
[IHostingStartup](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.ihostingstartup?view=aspnetcore-6.0) interface and adding a
[HostingStartup](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.hostingstartupattribute?view=aspnetcore-6.0) assembly attribute.  

Both the `IHostingStartup` interface and `HostingStartup` attribute are part of .NET core.  

> Nucleus overrides ASP.NET's normal behavior, which is to use a configuration file value to determine which startup classes to use.  Instead, Nucleus 
searches all extension assemblies for classes which implement `IHostingStartup` and adds them to the list of assemblies in the ASP.NET 
HostingStartupAssembliesKey configuration item.

> **_More Information:_**:  [Use hosting startup assemblies in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/platform-specific-configuration)

The IHostingStartup interface contains a `Configure(IWebHostBuilder builder)` method.  You can call `builder.ConfigureServices` to add services to the 
dependency injection container, and you can use `builder.Configure` to configure the HTTP request pipeline.

### Example
The example below is from the core Documents module.  This class adds services to dependency injection using a startup class, including a `ISystemEventHandler` to 
receive Nucleus system events - in this case, a MigrateEvent which is triggered after a data provider migration script is executed.  Some extensions use the MigrateEvent 
to perform post-installation or post-upgrade steps which can't be included in a database migration script.  

```
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Modules.Documents.DataProviders;
using Nucleus.Abstractions.Search;
using Nucleus.Data.EntityFramework;
using Nucleus.Data.Common;

[assembly:HostingStartup(typeof(Nucleus.Modules.Documents.Startup))]

namespace Nucleus.Modules.Documents
{

  public class Startup : IHostingStartup
  {
    public void Configure(IWebHostBuilder builder)
    {
      builder.ConfigureServices((context, services) => 
      {
        services.AddSingleton<DocumentsManager>();
        services.AddTransient<IContentMetaDataProducer, DocumentsMetaDataProducer>();
        services.AddDataProvider<IDocumentsDataProvider, DataProviders.DocumentsDataProvider, DataProviders.DocumentsDbContext>(context.Configuration);

        services.AddSingleton<Nucleus.Abstractions.EventHandlers.ISystemEventHandler<MigrateEvent, Migrate>, MigrationEventHandler>();
      });
    }
  }

  public class MigrationEventHandler : Nucleus.Abstractions.EventHandlers.ISystemEventHandler<MigrateEvent, Migrate>
  {
    public Task Invoke(MigrateEvent item)
    {
      if (item.SchemaName == "Nucleus.Modules.Documents")
      {
        if (item.ToVersion == new System.Version(1,0,0))
        {

        }
        // no implementation.  
        // This is a test and example of how you would execute code after a db schema migration.
      }
      return Task.CompletedTask;
    }
  }
}
```

> **_Note:_**:  The Data migration event (`MigrateEvent`) is sent to **all** subscribers, so you must check the `.SchemaName` property of the event to 
make sure that it applies to your extension.  

> **_Note:_**:  You aren't limited to adding Nucleus services to dependency injection, you can do anything you like.  But be careful, the actions that you execute 
in a startup class affect all of Nucleus, so you could potentially prevent Nucleus from successfully starting.
