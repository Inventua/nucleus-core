using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Modules.Forums.DataProviders;
using Nucleus.Data.EntityFramework;
using Nucleus.Data.Common;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Abstractions.Search;
using Nucleus.Abstractions.EventHandlers;

[assembly: HostingStartup(typeof(Nucleus.Modules.Forums.Startup))]

namespace Nucleus.Modules.Forums
{
  public class Startup : IHostingStartup
  {
    public void Configure(IWebHostBuilder builder)
    {
      builder.ConfigureServices((context, services) =>
      {
        // Add forum services, data provider
        services.AddSingleton<GroupsManager>();
        services.AddSingleton<ForumsManager>();
        services.AddDataProvider<IForumsDataProvider, DataProviders.ForumsDataProvider, DataProviders.ForumsDbContext>(context.Configuration);

        // search content producer
        services.AddTransient<IContentMetaDataProducer, ForumsMetaDataProducer>();

        // Event handlers manage the mail queue
        services.AddSingletonSystemEventHandler<Models.Post, Create, EventHandlers.CreatePostEventHandler>();

        services.AddSingletonSystemEventHandler<Models.Post, Create, EventHandlers.CreatePostEventHandler>();
        services.AddSingletonSystemEventHandler<Models.Reply, Create, EventHandlers.CreateReplyEventHandler>();

        services.AddSingletonSystemEventHandler<Models.Post, Approved, EventHandlers.ApprovedPostEventHandler>();
        services.AddSingletonSystemEventHandler<Models.Reply, Approved, EventHandlers.ApprovedReplyEventHandler>();

        services.AddSingletonSystemEventHandler<Models.Post, Rejected, EventHandlers.RejectedPostEventHandler>();
        services.AddSingletonSystemEventHandler<Models.Reply, Rejected, EventHandlers.RejectedReplyEventHandler>();

        // Handle migration (install) events
        // services.AddSingletonSystemEventHandler<MigrateEventArgs, MigrateEvent, MigrationEventHandler>();
      });
    }

  }
}

////	/// <summary>
////	/// Take action during installation events.
////	/// </summary>
////	/// <remarks>
////	/// When the forums data objects are created, create permission types for forums.
////	/// </remarks>
////	public class MigrationEventHandler : Nucleus.Abstractions.EventHandlers.ISingletonSystemEventHandler<MigrateEventArgs, MigrateEvent>
////	{
////		private IPermissionsManager PermissionsManager { get; }

////		public MigrationEventHandler(IPermissionsManager permissionsManager)
////		{
////			this.PermissionsManager = permissionsManager;
////		}

////		public Task Invoke(MigrateEventArgs item)
////		{
////			if (item.SchemaName == "Nucleus.Modules.Forums")
////			{
////        // we can retry creating forums permission types for every version, because PermissionsManager.AddPermissionType
////        // checks and does not throw an exception if the permission type already exists.
////        CreatePermissionType(new Guid("91E80ABF-29BD-4526-B054-8164080321A4"), ForumsManager.PermissionScopes.FORUM_VIEW, "View", 10);
////				CreatePermissionType(new Guid("3A81E8B0-7018-475D-ABDB-07A788468F78"), ForumsManager.PermissionScopes.FORUM_CREATE_POST, "Create Post", 20);
////				CreatePermissionType(new Guid("EF110F17-6178-438E-8F03-DC2D8A6A3134"), ForumsManager.PermissionScopes.FORUM_EDIT_POST, "Edit Post", 30);
////				CreatePermissionType(new Guid("3A9CDB24-A956-4D07-B79A-4380005E0E2C"), ForumsManager.PermissionScopes.FORUM_REPLY_POST, "Reply", 40);
////				CreatePermissionType(new Guid("8974847C-DFFC-411E-8699-D5B6965FDD8D"), ForumsManager.PermissionScopes.FORUM_DELETE_POST, "Delete", 50);
////				CreatePermissionType(new Guid("38D9B880-6977-4412-8CF5-B6A883161BDA"), ForumsManager.PermissionScopes.FORUM_LOCK_POST, "Lock", 60);
////				CreatePermissionType(new Guid("33E7380D-6BFC-4791-A675-CDFB60921054"), ForumsManager.PermissionScopes.FORUM_ATTACH_POST, "Attach", 70);
////				CreatePermissionType(new Guid("29455DD9-EADC-42D5-A2CD-5F2F5932D4D3"), ForumsManager.PermissionScopes.FORUM_SUBSCRIBE, "Subscribe", 80);
////				CreatePermissionType(new Guid("EEA8A7FD-55A4-4761-A0C2-415412FF73E0"), ForumsManager.PermissionScopes.FORUM_PIN_POST, "Pin", 90);
////				CreatePermissionType(new Guid("6B464A41-7F8F-4B61-8E96-251001F6A444"), ForumsManager.PermissionScopes.FORUM_MODERATE, "Moderate", 100);
////				CreatePermissionType(new Guid("84002ADD-71FF-44A9-92B1-67BA36E78CE4"), ForumsManager.PermissionScopes.FORUM_UNMODERATED, "Unmoderated", 110);
////			}
////			return Task.CompletedTask;
////		}

////	}
////}
