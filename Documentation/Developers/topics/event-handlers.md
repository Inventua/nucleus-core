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
To handle an event, create a class which implements the [ISystemEventHandler](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.EventHandlers.ISystemEventHandlerT0T1/)
interface.  

The functions used to raise and handle events both specify a TModel and TEvent.  The TModel type is the type of the model entity which has changed, and the TEvent 
type represents the event which occurred.  TEvent can be any type, but Nucleus has classes for [create](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.EventHandlers.SystemEventTypes.Create/),  
[update](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.EventHandlers.SystemEventTypes.Update/), and 
[delete](/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.EventHandlers.SystemEventTypes.Delete/).  If you need to represent a different kind of event, just 
create a class for it.  The TEvent class is used as a key in the dependency injection services collection, and does not need to inherit any class, or have any methods or properties.

### Example
The example below is from the core Documents module.  This class adds a  System Event Handler to receive Nucleus system events - in this case, a MigrateEvent which is triggered after a 
data provider migration script is executed.  Some extensions use the MigrateEvent to perform post-installation or post-upgrade steps which can't be included in a database migration script.  

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

        services.AddSingleton<Nucleus.Abstractions.EventHandlers.ISystemEventHandler<MigrateEventArgs, MigrateEvent>, MigrationEventHandler>();
      });
    }
  }

  public class MigrationEventHandler : Nucleus.Abstractions.EventHandlers.ISystemEventHandler<MigrateEventArgs, MigrateEvent>
  {
    public Task Invoke(MigrateEventArgs item)
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

> **_Note:_**:  The Data migration event [MigrateEvent](/api-documentation/Nucleus.Data.Common.xml/Nucleus.Data.Common.MigrateEvent/) 
is sent to **all** subscribers, so you must check the `.SchemaName` property of the [MigrateEventArgs](/api-documentation/Nucleus.Data.Common.xml/Nucleus.Data.Common.MigrateEventArgs/) 
object that is passed to the ==Invoke== method to make sure that it applies to your extension.  Data schema migration doesn't happen immediately 
when you install an update, it runs the first time the extension accesses the database.