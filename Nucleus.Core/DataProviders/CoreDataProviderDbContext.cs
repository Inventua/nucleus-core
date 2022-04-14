using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nucleus.Data.EntityFramework;

namespace Nucleus.Core.DataProviders
{
	/// <summary>
	/// Nucleus core entity-framework data context.
	/// </summary>
	public class CoreDataProviderDbContext : Nucleus.Data.EntityFramework.DbContext
	{
		// DBSets

		public DbSet<PermissionType> PermissionTypes { get; set; }
		public DbSet<Permission> Permissions { get; set; }

		public DbSet<Site> Sites { get; set; }

		public DbSet<SiteAlias> SiteAlias { get; set; }

		public DbSet<SiteSetting> SiteSettings { get; set; }

		public DbSet<UserProfileProperty> UserProfileProperties { get; set; }

		public DbSet<List> Lists { get; set; }

		public DbSet<ListItem> ListItems { get; set; }


		public DbSet<SiteGroup> SiteGroups { get; set; }

		public DbSet<UserSession> UserSessions { get; set; }

		public DbSet<ScheduledTask> ScheduledTasks { get; set; }
		public DbSet<ScheduledTaskHistory> ScheduledTaskHistory { get; set; }

		public DbSet<Nucleus.Abstractions.Models.FileSystem.Folder> Folders { get; set; }

		public DbSet<Nucleus.Abstractions.Models.FileSystem.File> Files { get; set; }

		public DbSet<Nucleus.Abstractions.Models.Mail.MailTemplate> MailTemplates { get; set; }

		public DbSet<ModuleDefinition> ModuleDefinitions { get; set; }

		public DbSet<ContainerDefinition> ContainerDefinitions { get; set; }

		public DbSet<LayoutDefinition> LayoutDefinitions { get; set; }

		public DbSet<ControlPanelExtensionDefinition> ControlPanelExtensionDefinitions { get; set; }

		public DbSet<Role> Roles{ get; set; }

		public DbSet<RoleGroup> RoleGroups { get; set; }

		public DbSet<Page> Pages { get; set; }
		public DbSet<PageModule> PageModules { get; set; }

		public DbSet<Content> Contents { get; set; }

		public DbSet<User> Users { get; set; }

		public DbSet<ApiKey> ApiKeys { get; set; }

		// This isn't referenced anywhere, but it allows EF to correctly understand the database schema
		public DbSet<UserSecrets> UserSecrets { get; set; }

		public CoreDataProviderDbContext(DbContextConfigurator<CoreDataProvider> dbContextConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory) 
			: base(dbContextConfigurator, httpContextAccessor, loggerFactory)	{	}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <remarks>
		/// This constructor exists for use by the entity framework tools only.  Do not use this constructor.
		/// </remarks>
		//public CoreDataProviderDbContext(DbContextOptions options): base(options, new DbContextConfigurator<CoreDataProviderDbContext>)
		//{
		//}

		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			if (options == null)
				throw new ArgumentException("options cannot be null.", nameof(options));
			base.OnConfiguring(options);
		}

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);
		}
	}
}
