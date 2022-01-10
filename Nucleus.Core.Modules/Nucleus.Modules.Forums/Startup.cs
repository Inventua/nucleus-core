using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Modules.Forums.DataProviders;
using Nucleus.Data.EntityFramework;
using Nucleus.Data.Common;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;

[assembly: HostingStartup(typeof(Nucleus.Modules.Forums.Startup))]

namespace Nucleus.Modules.Forums
{
	public class Startup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) =>
			{
				services.AddSingleton<GroupsManager>();
				services.AddSingleton<ForumsManager>();
				services.AddDataProvider<IForumsDataProvider, DataProviders.ForumsDataProvider, DataProviders.ForumsDbContext>(context.Configuration);

				services.AddSingleton<Nucleus.Abstractions.EventHandlers.ISystemEventHandler<MigrateEvent, Migrate>, MigrationEventHandler>();

			});
		}
	}
	public class MigrationEventHandler : Nucleus.Abstractions.EventHandlers.ISystemEventHandler<MigrateEvent, Migrate>
	{
		private IPermissionsManager PermissionsManager { get; }

		public MigrationEventHandler(IPermissionsManager permissionsManager)
		{
			this.PermissionsManager = permissionsManager;
		}

		public Task Invoke(MigrateEvent item)
		{
			if (item.SchemaName == "Nucleus.Modules.Forums")
			{
				if (item.FromVersion == new System.Version(0, 0, 0, 0))
				{
					CreatePermissionType(new Guid("91E80ABF-29BD-4526-B054-8164080321A4"), "urn:nucleus:entities:forum/permissiontype/view", "View", 0);
					CreatePermissionType(new Guid("3A81E8B0-7018-475D-ABDB-07A788468F78"), "urn:nucleus:entities:forum/permissiontype/createpost", "Post", 1);
					CreatePermissionType(new Guid("3A9CDB24-A956-4D07-B79A-4380005E0E2C"), "urn:nucleus:entities:forum/permissiontype/reply", "Reply", 2);
					CreatePermissionType(new Guid("8974847C-DFFC-411E-8699-D5B6965FDD8D"), "urn:nucleus:entities:forum/permissiontype/delete", "Delete", 3);
					CreatePermissionType(new Guid("38D9B880-6977-4412-8CF5-B6A883161BDA"), "urn:nucleus:entities:forum/permissiontype/lock", "Lock", 4);
					CreatePermissionType(new Guid("33E7380D-6BFC-4791-A675-CDFB60921054"), "urn:nucleus:entities:forum/permissiontype/attach", "Attach", 5);
					CreatePermissionType(new Guid("29455DD9-EADC-42D5-A2CD-5F2F5932D4D3"), "urn:nucleus:entities:forum/permissiontype/subscribe", "Subscribe", 6);
					CreatePermissionType(new Guid("EEA8A7FD-55A4-4761-A0C2-415412FF73E0"), "urn:nucleus:entities:forum/permissiontype/pin", "Pin", 7);
					CreatePermissionType(new Guid("6B464A41-7F8F-4B61-8E96-251001F6A444"), "urn:nucleus:entities:forum/permissiontype/moderate", "Moderate", 8);
				}
			}
			return Task.CompletedTask;
		}

		private void CreatePermissionType(Guid id, string scope, string name, int sortOrder)
		{
			this.PermissionsManager.AddPermissionType(new Abstractions.Models.PermissionType()
			{
				Id = id, 
				Scope = scope,
				Name = name,
				SortOrder=sortOrder
			});
		}
	}
}
