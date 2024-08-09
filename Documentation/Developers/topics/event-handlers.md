## Event Handlers
Nucleus Extensions implement an Event Handler to execute code when a system or extension event is raised, and can raise events of their own
to be handled within the extension, or by another extension.  The data provider for most Nucleus core entities 
raises events when data is changed (when a record is created, modified or deleted), and the Nucleus entity framework 
[migration](/api-documentation/Nucleus.Data.Common.xml/Nucleus.Data.Common.DataProviderMigration/) class raises 
events when a database schema migration is completed.

### Raising an Event
To raise an event, add a reference to the Nucleus [IEventDispatcher](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.EventHandlers.IEventDispatcher/)
to your constructor.  Call [RaiseEvent](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.EventHandlers.IEventDispatcher/#RaiseEventTModelTEvent(TModel)) to 
raise an event.  

### Handling an Event
To handle an event, create a class which implements the [ISystemEventHandler](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.EventHandlers.IScopedSystemEventHandlerT0T1/)
or ISingletonSystemEventHandler interface.  

> The IScopedSystemEventHandler and ISingletonSystemEventHandler interfaces were added in Nucleus 1.3.  Previously, extensions could implement ISystemEventHandler, but
> this caused problems when retrieving event handler implementations from dependency injection when there are both scoped and 
> singleton handlers for the same model and event.

The functions used to raise and handle events both specify a `TModel` and `TEvent`.  The TModel type is the type of the model entity which has changed, and the TEvent 
type represents the event which occurred.  TEvent can be any type, but Nucleus has classes for 
[create](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.EventHandlers.SystemEventTypes.Create/), [update](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.EventHandlers.SystemEventTypes.Update/), and 
[delete](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.EventHandlers.SystemEventTypes.Delete/).  If you need to represent a different kind of event, just 
create a class for it. The TEvent class is used as a key in the dependency injection services collection, and does not need to inherit any class, or 
have any methods or properties.

### Example
The example below is from the Static Content module ([source code](https://github.com/Inventua/nucleus-core/tree/main/Nucleus.Core.Modules/Nucleus.Modules.StaticContent)). 
This class adds a scoped System Event Handler to receive Nucleus system events - in this case, an Update event which is triggered after a 
file is updated.

```
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;

[assembly: HostingStartup(typeof(Nucleus.Modules.StaticContent.Startup))]

namespace Nucleus.Modules.StaticContent;

public class Startup : IHostingStartup
{
  public void Configure(IWebHostBuilder builder)
  {
    builder.ConfigureServices((context, services) =>
    {
      services.AddScoped<Nucleus.Abstractions.EventHandlers.IScopedSystemEventHandler<Nucleus.Abstractions.Models.FileSystem.File, Nucleus.Abstractions.EventHandlers.SystemEventTypes.Update>, FileEventHandler>();
    });
  }
}

public class FileEventHandler : Nucleus.Abstractions.EventHandlers.IScopedSystemEventHandler<Nucleus.Abstractions.Models.FileSystem.File, Nucleus.Abstractions.EventHandlers.SystemEventTypes.Update>
{
  private ICacheManager CacheManager { get; }
  private IExtensionManager ExtensionManager { get; }
  private Context Context { get; }

  public FileEventHandler(Context context, ICacheManager cacheManager, IExtensionManager extensionManager)
  {
    this.Context = context;
    this.CacheManager = cacheManager;
    this.ExtensionManager = extensionManager;
  }

  public async Task Invoke(Nucleus.Abstractions.Models.FileSystem.File file)
  {
    // if a file changes, clear the static content cache for static content modules which use the specified file

    // This must match the value in package.xml
    Guid moduleDefinitionId = Guid.Parse("0930d4fe-0469-47e6-a28b-7c42d85a61fd");

    foreach (PageModule module in await this.ExtensionManager.ListPageModules(new Nucleus.Abstractions.Models.ModuleDefinition() { Id = moduleDefinitionId }))
    {
      Models.Settings settings = new();

      settings.ReadSettings(module);

      if (settings.DefaultFileId == file.Id)
      {
        this.CacheManager.StaticContentCache().Remove(this.Context.Site.Id + ":" + file.Id);
      }
    }
  }
}
```

> **_Note:_** The Data migration event [MigrateEvent](/api-documentation/Nucleus.Data.Common.xml/Nucleus.Data.Common.MigrateEvent/) 
is sent to **all** subscribers, so you must check the `.SchemaName` property of the [MigrateEventArgs](/api-documentation/Nucleus.Data.Common.xml/Nucleus.Data.Common.MigrateEventArgs/) 
object that is passed to the ==Invoke== method to make sure that it applies to your extension.  Data schema migration doesn't happen immediately 
when you install an update, it runs the first time the extension accesses the database.