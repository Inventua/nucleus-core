## Startup classes
Startup classes add functionality to Nucleus during startup, by providing an interface which extensions use to add classes to the dependency injection
container. Use Startup classes to:
- Register an extension's data provider.
- Register an [Event Handler](https://www.nucleus-cms.com/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.EventHandlers.IEventDispatcher/).
- Register Nucleus [Search Providers](https://www.nucleus-cms.com/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.Search.ISearchProvider/), 
[Search Content Providers](https://www.nucleus-cms.com/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.Search.IContentMetaDataProducer/)
and [Search Index Managers](https://www.nucleus-cms.com/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.Search.ISearchIndexManager/).
- Add other services to dependency injection, like adding a [Filter](https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters).

Extensions can execute code during startup.  In order to create a startup class, your assembly must have a 
[HostingStartup](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.hostingstartupattribute) attribute and 
must contain a class which implements the [IHostingStartup](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.ihostingstartup)
interface.

> The [HostingStartup](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.hostingstartupattribute) attribute and 
[IHostingStartup](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.ihostingstartup) interface are a standard part of 
ASP.NET core.  Refer to [Use hosting startup assemblies in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/platform-specific-configuration)
for more information.
The only difference in Nucleus is that all assemblies in extension folders which have a 
[HostingStartup](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.hostingstartupattribute) attribute are automatically 
added during startup, and do not need to be configured using the
[WebHostDefaults.HostingStartupAssembliesKey](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.webhostdefaults.hostingstartupassemblieskey)
configuration key.

> Your [IHostingStartup](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.ihostingstartup) implementation is run before Nucleus 
adds any services to dependency injection.  You can execute code in a startup class which will prevent Nucleus from starting successfully, 
so take care if you are doing anything that isn't documented as a standard activity in a startup class.  In general, you will add a delegate
for configuring additional services by calling [IWebHostBuilder.ConfigureServices](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.iwebhostbuilder.configureservices).  
Don't add a call to IWebHostBuilder.Configure - this does not add a delegate (additional code to execute during startup), it completely replaces 
the Nucleus .Configure code and will stop Nucleus from successfully starting.

This code is from the Elastic Search extension.  It adds a search index manager and search provider.
```
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Search;

[assembly: HostingStartup(typeof(Nucleus.Extensions.ElasticSearch.Startup))]

namespace Nucleus.Extensions.ElasticSearch
{
  public class Startup : IHostingStartup
  {
    public void Configure(IWebHostBuilder builder)
    {
      builder.ConfigureServices((context, services) =>
      {
        services.AddTransient<ISearchIndexManager, SearchIndexManager>();
        services.AddTransient<ISearchProvider, SearchProvider>();
      });
    }
  }
}
```

This code is from the ==Accept Terms== module.  It adds the module's manager class and data provider classes to dependency injection.
```
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Data.EntityFramework;
using Nucleus.Modules.AcceptTerms.DataProviders;

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
```