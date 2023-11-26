## Data Providers
Create a data provider class in order to communicate with the database.  Most data providers will use [Entity Framework](https://docs.microsoft.com/en-us/ef/), so they inherit 
the Nucleus entity framework [DataProvider](/api-documentation/Nucleus.Data.EntityFramework.xml/Nucleus.Data.EntityFramework.DataProvider/).  The 
Nucleus entity framework data provider base class provides [schema migration](/developers/database-scripts/) and a common location for 
[database configuration](/configuration-files/#databasesettings.json) information.

> **Note**: You don't have to use entity framework.  You can inherit [DataProvider](/api-documentation/Nucleus.Data.Common.xml/Nucleus.Data.Common.DataProvider/)
from [Nucleus.Data.Common](/api-documentation/Nucleus.Data.Common.xml/#asm_Nucleus-Data-Common/) if you aren't using Entity Framework, or you 
can bypass the Nucleus database provider classes altogether and use your own code to access data.  The rest of this page describes the use of 
entity framework-based data providers.

> **Note**: Extensions have ==data providers==, which provide extension-specific data access functions.  Nucleus has ==Database Providers== which allow your data provider 
to use SQL Server, SQLite, MySql and PostgreSQL.  By inheriting the Nucleus-provided [DataProvider](/api-documentation/Nucleus.Data.EntityFramework.xml/Nucleus.Data.EntityFramework.DataProvider/) 
and [DbContext](/api-documentation/Nucleus.Data.EntityFramework.xml/Nucleus.Data.EntityFramework.DbContext/) classes, your data provider is automatically configured, all 
you need to do is add your data provider to dependency injection and Nucleus takes care of the rest.

## Creating your data provider
When using entity framework, your data provider inherits [DataProvider](/api-documentation/Nucleus.Data.EntityFramework.xml/Nucleus.Data.EntityFramework.DataProvider/) 
and has methods which use a DbContext.  Inherit [DbContext](/api-documentation/Nucleus.Data.EntityFramework.xml/Nucleus.Data.EntityFramework.DbContext/), which 
has extra functionality so that Nucleus can automatically configure the correct database provider, including automatic database schema [migrations](/developers/database-scripts/).

Your DbContext implementation will be the same as a standard entity framework [DbContext](https://docs.microsoft.com/en-us/dotnet/api/system.data.entity.dbcontext) with 
one or more [DbSet](https://docs.microsoft.com/en-us/dotnet/api/system.data.entity.dbset-1) properties, and an 
[OnModelCreating](https://docs.microsoft.com/en-us/dotnet/api/system.data.entity.dbcontext.onmodelcreating) override to tell entity framework more about 
your model classes and database objects.

Your [DataProvider](/api-documentation/Nucleus.Data.EntityFramework.xml/Nucleus.Data.EntityFramework.DataProvider/) consists of functions which interact with the database.

{.file-name}
### Data Provider Example
```
using System;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Modules.Documents.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Nucleus.Modules.Documents.DataProviders;

public class DocumentsDataProvider : Nucleus.Data.EntityFramework.DataProvider, IDocumentsDataProvider
{
  protected IEventDispatcher EventManager { get; }
  protected new DocumentsDbContext Context { get; }

  public DocumentsDataProvider(DocumentsDbContext context, IEventDispatcher eventManager, ILogger<DocumentsDataProvider> logger) : base(context, logger)
  {
    this.EventManager = eventManager;
    this.Context = context;
  }

  public async Task<Document> Get(Guid id)
  {
    return await this.Context.Documents
      .Where(document => document.Id == id)
      .Include(document => document.Category)
      .Include(document => document.File)
      .AsNoTracking()
      .FirstOrDefaultAsync();
  }
    
  public async Task<IList<Document>> List(PageModule pageModule)
  {
    return await this.Context.Documents
      .Where(document => EF.Property<Guid>(document, "ModuleId") == pageModule.Id)
      .Include(document => document.Category)
      .Include(document => document.File)
      .AsNoTracking()
      .AsSingleQuery()
      .OrderBy(document => document.SortOrder)
      .ToListAsync();
  }
}
```

{.file-name}
### DbContext Example
```
using System;
using Nucleus.Data.EntityFramework;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Nucleus.Modules.Documents.Models;

namespace Nucleus.Modules.Documents.DataProviders;

public class DocumentsDbContext : Nucleus.Data.EntityFramework.DbContext
{
  public DbSet<Document> Documents { get; set; }

  public DocumentsDbContext(DbContextConfigurator<DocumentsDataProvider> dbConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory) : base(dbConfigurator, httpContextAccessor, loggerFactory)  {  }

  /// <summary>
  /// Configure entity framework with schema information that it cannot automatically detect.
  /// </summary>
  /// <param name="builder"></param>
  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder.Entity<Document>().Property<Guid>("ModuleId");

    builder.Entity<Document>()
      .HasOne(document => document.Category)
      .WithMany()
      .HasForeignKey("CategoryId");

    builder.Entity<Document>()
      .HasOne(document => document.File)
      .WithMany()
      .HasForeignKey("FileId");
  }
}
```

## Adding your data provider to Dependency Injection
Your data provider must be added to dependency injection so that your controllers can use it.  Use the 
[AddDataProvider](/api-documentation/Nucleus.Data.Common.xml/Nucleus.Data.Common.DataProviderExtensions/#AddDataProviderTDataProvider(IServiceCollectionIConfiguration)) 
extension method provided by the [Nucleus.Data.Common](/api-documentation/Nucleus.Data.Common.xml/#asm_Nucleus-Data-Common/) in a 
[Startup Class](/developers/startup-classes/) to add and configure your data provider and associated objects.

### Example
The example below is from the core Documents module.  Code which does not demonstrate adding a data provider has been removed 
for brevity.

```
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Modules.Documents.DataProviders;
using Nucleus.Abstractions.Search;
using Nucleus.Data.EntityFramework;

[assembly:HostingStartup(typeof(Nucleus.Modules.Documents.Startup))]

namespace Nucleus.Modules.Documents;

public class Startup : IHostingStartup
{
  public void Configure(IWebHostBuilder builder)
  {
    builder.ConfigureServices((context, services) => 
    {
      services.AddDataProvider<IDocumentsDataProvider, DataProviders.DocumentsDataProvider, DataProviders.DocumentsDbContext>(context.Configuration);
    });
  }
}
```

