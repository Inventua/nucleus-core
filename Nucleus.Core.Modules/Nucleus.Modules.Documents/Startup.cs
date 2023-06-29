using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        ////services.AddSingletonSystemEventHandler<MigrateEventArgs, MigrateEvent, MigrationEventHandler>();
      });
		}
	}


	////public class MigrationEventHandler : Nucleus.Abstractions.EventHandlers.ISystemEventHandler<MigrateEventArgs, MigrateEvent>
	////{
	////	public Task Invoke(MigrateEventArgs item)
	////	{
	////		if (item.SchemaName == "Nucleus.Modules.Documents")
	////		{
	////			if (item.ToVersion == new System.Version(1,0,0))
	////			{

	////			}
	////			// no implementation.  
	////			// This is a test and example of how you would execute code after a db schema migration.
	////		}
	////		return Task.CompletedTask;
	////	}
	////}
}
