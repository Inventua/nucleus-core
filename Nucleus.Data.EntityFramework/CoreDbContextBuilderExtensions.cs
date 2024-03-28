using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Abstractions.Search;

namespace Nucleus.Data.EntityFramework
{
	/// <summary>
	/// Extension methods used to configure the core DbContext used by data providers which inherit the entity framework <see cref="DataProvider"/>.
	/// </summary>
	public static class CoreDbContextBuilderExtensions
	{
    /// <summary>
		/// Configure schema entity.
		/// </summary>
		/// <param name="builder"></param>
		/// <returns></returns>
		public static ModelBuilder ConfigureSchemaEntities(this ModelBuilder builder)
    {
      builder.Entity<Nucleus.Abstractions.Models.Internal.Schema>()
        .ToTable("Schema")
        .HasKey(schema => schema.SchemaName);
      
      return builder;
    }

    /// <summary>
    /// Configure fundamental core entities.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static ModelBuilder ConfigureInstanceEntities(this ModelBuilder builder)
		{
			builder.Entity<ModuleDefinition>().ToTable("ModuleDefinitions");
      builder.Entity<ModuleDefinition>().Property<String>("ClassTypeName");

      builder.Entity<LayoutDefinition>().ToTable("LayoutDefinitions");
			builder.Entity<ContainerDefinition>().ToTable("ContainerDefinitions");
			builder.Entity<ControlPanelExtensionDefinition>().ToTable("ControlPanelExtensionDefinitions");

			builder.Entity<ScheduledTask>().ToTable("ScheduledTasks");
			builder.Entity<ScheduledTaskHistory>().ToTable("ScheduledTaskHistory");

			builder.Entity<ApiKey>().ToTable("ApiKeys");

      builder.Entity<ExtensionsStoreSettings>().ToTable("ExtensionsStoreSettings")
        .HasKey(settings => settings.StoreUri);

      builder.Entity<SearchIndexHistory>().ToTable("SearchIndexHistory")
        .HasKey(history => new { history.SiteId, history.Scope, history.SourceId });

      return builder;
		}

		/// <summary>
		/// Configure site and related entities like role, role group, mail template
		/// </summary>
		/// <param name="builder"></param>
		/// <returns></returns>
		public static ModelBuilder ConfigureSiteEntities(this ModelBuilder builder)
		{
			builder.Entity<Site>()
				.ToTable("Sites");

			// Entity-framework can't deal with both a one-to-one(default site alias) and one-to-many(aliases) relationship between the same two
			// tables.  Mark site.DefaultSiteAlias as ignored, add a shadow property DefaultSiteAliasId and read DefaultSiteAlias manually in 
			// the data provider.  
			builder.Entity<Site>()
				.Ignore(site => site.DefaultSiteAlias);
			builder.Entity<Site>().Property<Guid?>("DefaultSiteAliasId");

			// Add SiteId shadow property and set as required so cascade deletes work
			builder.Entity<SiteAlias>()
				.ToTable("SiteAlias")
				.HasOne<Site>()
				.WithMany(site => site.Aliases)
				.HasForeignKey("SiteId").IsRequired();
			
			// SiteId is not a CLR property (it is a shadow property) so we must define its type
			builder.Entity<SiteSetting>()
				.ToTable("SiteSettings")
				.Property<Guid>("SiteId").IsRequired();
				
			// define two-part primary key for <SiteSetting>
			builder.Entity<SiteSetting>()
				.HasKey(new string[] { "SiteId", "SettingName" });
			builder.Entity<SiteSetting>()
				.HasOne<Site>()
				.WithMany(site => site.SiteSettings)
				.HasForeignKey("SiteId");

			// Add SiteId shadow property
			builder.Entity<Role>()
				.ToTable("Roles")
				.HasOne<Site>();

			// Add SiteId shadow property
			builder.Entity<RoleGroup>()
				.ToTable("RoleGroups")
				.HasOne<Site>();

			// Add SiteId shadow property
			builder.Entity<Nucleus.Abstractions.Models.Mail.MailTemplate>()
				.ToTable("MailTemplates")
				.HasOne<Site>();

			// Add SiteId shadow property
			builder.Entity<List>()
				.ToTable("Lists")
				.HasOne<Site>();

			builder.Entity<ListItem>().ToTable("ListItems");

			builder.Entity<UserProfileProperty>()
				.ToTable("UserProfileProperties")
				.HasOne<Site>()
				.WithMany(site => site.UserProfileProperties)
				.HasForeignKey("SiteId").IsRequired();

			builder.Entity<Permission>().ToTable("Permissions");
			builder.Entity<PermissionType>().ToTable("PermissionTypes");
			
			return builder;
		}

		/// <summary>
		/// Configure page and related entities
		/// </summary>
		/// <param name="builder"></param>
		/// <returns></returns>
		public static ModelBuilder ConfigurePageEntities(this ModelBuilder builder)
		{
			builder.Entity<Page>()
				.ToTable("Pages")
				.Ignore(prop => prop.IsFirst)
				.Ignore(prop => prop.IsLast)
				//.Ignore(page => page.Permissions)
				.HasOne<Site>();

      builder.Entity<Page>()
        .HasMany(page => page.Permissions)
        .WithOne()
        .HasForeignKey(perm => perm.RelatedId);

      builder.Entity<PageRoute>()
				.ToTable("PageRoutes")
				.HasOne<Page>()
				.WithMany(page => page.Routes);

			builder.Entity<PageRoute>().Property<Guid>("SiteId");

			return builder;
		}

		/// <summary>
		/// Configure module and related entities
		/// </summary>
		/// <param name="builder"></param>
		/// <returns></returns>

		public static ModelBuilder ConfigureModuleEntities(this ModelBuilder builder)
		{
			// Entity framework can't deal the permissions table which can have many types of parent entuty, so we exclude
			// the permissions properties and handle manually in the data provider
			builder.Entity<PageModule>()
				.ToTable("PageModules")
				.Ignore(module => module.Permissions)
				.HasOne<Page>()
				.WithMany(page => page.Modules);

      builder.Entity<PageModule>()
        .HasMany(module => module.Permissions)
        .WithOne()
        .HasForeignKey(perm => perm.RelatedId);

      // PageModuleId is not a CLR property (it is a shadow property) so we must define its type.  The relationship
      // is already inferred because of the Page.ModuleSettings property
      builder.Entity<ModuleSetting>()
				.ToTable("PageModuleSettings")
				.Property<Guid>("PageModuleId");

			// configure relationship between pagemodule and modulesetting
			builder.Entity<ModuleSetting>()
				.HasOne<PageModule>()
				.WithMany(pageModule => pageModule.ModuleSettings);

			// define two-part primary key for <ModuleSetting>
			builder.Entity<ModuleSetting>()
				.HasKey(new string[] { "PageModuleId", "SettingName" });

			builder.Entity<Content>().ToTable("Content");

			return builder;
		}

		/// <summary>
		/// Configure user and related entities
		/// </summary>
		/// <param name="builder"></param>
		/// <returns></returns>
		public static ModelBuilder ConfigureUserEntities(this ModelBuilder builder)
		{
			builder.Entity<User>()
				.ToTable("Users")
				.HasOne(user => user.Secrets);

			// Custom valid converter for userSession.RemoteIpAddress [System.Net.IPAddress] is required because the PostgreSQL E/F provider can't convert
			// string to IPAddress.
			builder.Entity<UserSession>()
				.ToTable("UserSessions")
				.Property(userSession => userSession.RemoteIpAddress)
				.HasConversion(
						value => value.ToString(),
						value => System.Net.IPAddress.Parse(value));

			builder.Entity<UserProfileValue>()
				.ToTable("UserProfileValues")
				.HasKey(new string[] { "UserId", "UserProfilePropertyId" });

			builder.Entity<UserSecrets>()
				.ToTable("UserSecrets")
				.Property<Guid>("UserId");

			builder.Entity<UserSecrets>()
				.Property(userSecrets => userSecrets.PasswordResetToken).IsRequired(false);
			
			builder.Entity<UserSecrets>()
				.HasKey("UserId");

			// Define the user-role many-to-many relationship as shadow table/properties
			builder.Entity<User>()
				.HasMany<Role>(user => user.Roles)
				.WithMany(role => role.Users)
				.UsingEntity(entity =>
				{
					entity.Property<Guid>("UserId");
					entity.Property<Guid>("RoleId");
					entity.HasKey("UserId", "RoleId");
					entity.ToTable("UserRoles");
				});

      // SiteId is not a CLR property (it is a shadow property) so we must define its type
     builder.Entity<Organization>()
        .ToTable("Organizations")
        .Property<Guid>("SiteId").IsRequired();

      builder.Entity<OrganizationUser>()
        .ToTable("OrganizationUsers")
        .HasKey(new string[] { "OrganizationId", "UserId" });

      return builder;
		}

		/// <summary>
		/// Configure file system related entities
		/// </summary>
		/// <param name="builder"></param>
		/// <returns></returns>
		public static ModelBuilder ConfigureFileSystemEntities(this ModelBuilder builder)
		{
			// Entity framework can't deal the permissions table which can have many parents, so we exclude
			// the permissions properties and handle manually in the data provider.  Also ignore folder
			// properties which are not stored in the database (they are read from the file system)
			builder.Entity<Nucleus.Abstractions.Models.FileSystem.Folder>()
				.ToTable("Folders")
				.Ignore(folder => folder.Name)
				.Ignore(folder => folder.DateModified)
				.Ignore(folder => folder.IsSelected)
				.Ignore(folder => folder.Folders)
				.Ignore(folder => folder.Files)
				.Ignore(folder => folder.Parent)
				.Ignore(folder => folder.Permissions)
				.Ignore(folder => folder.Capabilities)
				.Ignore(folder => folder.FolderValidationRules)
				.Ignore(folder => folder.FileValidationRules);

			// Add SiteId shadow property
			builder.Entity<Nucleus.Abstractions.Models.FileSystem.Folder>()
				.HasOne<Site>();

			// Ignore file properties which are not stored in the database (they are read from the file system)
			builder.Entity<Nucleus.Abstractions.Models.FileSystem.File>()
				.ToTable("Files")
				.Ignore(file => file.Name)
				.Ignore(file => file.DateModified)
				.Ignore(file => file.IsSelected)
				.Ignore(file => file.Parent)
				.Ignore(file => file.Size)
				.Ignore(folder => folder.Capabilities);

			// Add SiteId shadow property
			builder.Entity<Nucleus.Abstractions.Models.FileSystem.File>()
				.HasOne<Site>();
						
			return builder;
		}
	}
}
