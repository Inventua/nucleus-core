using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nucleus.Data.Common;
using Nucleus.Data.EntityFramework;
using Nucleus.DNN.Migration.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.DataProviders;

public class DNNDbContext : Nucleus.Data.EntityFramework.DbContext
{
  public DbSet<Models.DNN.Version> Version { get; set; }
  public DbSet<Models.DNN.Portal> Portals { get; set; }

  public DbSet<Models.DNN.RoleGroup> RoleGroups { get; set; }
  public DbSet<Models.DNN.Role> Roles { get; set; }

  public DbSet<Models.DNN.Page> Pages { get; set; }

  public DbSet<Models.DNN.PageModuleSetting> PageModuleSettings { get; set; }

  public DbSet<Models.DNN.User> Users { get; set; }

  public DbSet<Models.DNN.UserRole> UserRoles { get; set; }
  public DbSet<Models.DNN.UserPortal> UserPortals { get; set; }

  public DbSet<Models.DNN.File> Files { get; set; }
  public DbSet<Models.DNN.Folder> Folders { get; set; }

  public DbSet<Models.DNN.ListItem> ListItems { get; set; }



  public DbSet<Models.DNN.UserProfileProperty> UserProfileProperties { get; set; }
  public DbSet<Models.DNN.UserProfilePropertyDefinition> UserProfilePropertyDefinitions { get; set; } 

  public DbSet<Models.DNN.Modules.TextHtml> TextHtml { get; set; }
  public DbSet<Models.DNN.Modules.Document> Documents { get; set; }
  public DbSet<Models.DNN.Modules.DocumentsSettings> DocumentsSettings { get; set; }
  public DbSet<Models.DNN.Modules.MediaSettings> MediaSettings { get; set; }
  public DbSet<Models.DNN.Modules.Link> Links { get; set; }

  public DbSet<Models.DNN.Modules.Blog> Blogs { get; set; }

  public DbSet<Models.DNN.Modules.ForumGroup> ForumGroups { get; set; }
  public DbSet<Models.DNN.Modules.Forum> Forums { get; set; }

  public DbSet<Models.DNN.Modules.ForumPost> ForumPosts { get; set; }

  public DNNDbContext(DbContextConfigurator<DNNDataProvider> dbConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory) : base(dbConfigurator, httpContextAccessor, loggerFactory)
  {
    this.ConfigureNucleusEntities = false;
  }

  protected override void OnConfiguring(DbContextOptionsBuilder options)
  {
    base.OnConfiguring(options);
  }

  /// <summary>
  /// Configure entity framework with schema information that it cannot automatically detect.
  /// </summary>
  /// <param name="builder"></param>
  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder.Entity<Models.DNN.RoleGroup>()
      .HasMany(group => group.Roles);

    builder.Entity<Models.DNN.Role>()
      .HasMany(role => role.Users)
      .WithMany(user => user.Roles)
      .UsingEntity<Models.DNN.UserRole>(entity => 
      {
        entity.Property(ur => ur.UserId);
        entity.Property(ur => ur.RoleId);
        entity.HasKey(ur => new { ur.UserId, ur.RoleId });
        entity.ToTable("UserRoles");
      });

    builder.Entity<Models.DNN.Portal>()
      .HasKey(portal => portal.PortalId);

    builder.Entity<Models.DNN.Portal>()
      .ToView("vw_Portals");

    builder.Entity<Models.DNN.User>()
      .HasMany(user => user.Roles)
      .WithMany(role => role.Users)
      .UsingEntity<Models.DNN.UserRole>(entity =>
      {
        entity.Property(ur => ur.UserId);
        entity.Property(ur => ur.RoleId);
        entity.HasKey(ur => new { ur.UserId, ur.RoleId });
        entity.ToTable("UserRoles");
      })
      .HasMany(user => user.ProfileProperties)
      .WithOne(profileproperty => profileproperty.User);

    builder.Entity<Models.DNN.UserProfilePropertyDefinition>()
      .ToTable("ProfilePropertyDefinition")
      .HasKey(userProfilePropertyDefinition => userProfilePropertyDefinition.PropertyDefinitionId);

    builder.Entity<Models.DNN.UserPortal>()
      .ToTable("UserPortals")
      .HasKey(userPortal => userPortal.UserPortalId);

    builder.Entity<Models.DNN.UserProfileProperty>()
      .ToTable("UserProfile")
      .HasKey(userProfileProperty => userProfileProperty.ProfileId);

    builder.Entity<Models.DNN.Page>()
     .ToTable("Tabs");

    builder.Entity<Models.DNN.Page>()
      .HasMany(page => page.Permissions)
      .WithOne(permission => permission.Page)
      .HasForeignKey("TabID");

    builder.Entity<Models.DNN.Page>()
      .HasMany(page => page.PageModules)
      .WithOne(module => module.Page)
      .HasForeignKey("TabID");

    builder.Entity<Models.DNN.PagePermission>()
      .ToView("vw_TabPermissions")
      .HasKey(tabPermission => tabPermission.TabPermissionId);

    builder.Entity<Models.DNN.PageModule>()
      .ToView("vw_Modules")
      .HasKey(module => module.TabModuleId);

    builder.Entity<Models.DNN.PageModule>()
      .HasMany(module => module.Permissions)
      .WithOne(permission => permission.PageModule)
      .HasForeignKey("ModuleID");

    builder.Entity<Models.DNN.PageModuleSetting>()
      .ToTable("ModuleSettings")
      .HasKey(setting => new { setting.ModuleId, setting.SettingName});

    builder.Entity<Models.DNN.PageModulePermission>()
      .ToView("vw_ModulePermissions")
      .HasKey(modulePermission => modulePermission.ModulePermissionId);

    builder.Entity<Models.DNN.DesktopModule>()
      .ToTable("DesktopModules")
      .HasKey(desktopModule => desktopModule.DesktopModuleId);

    builder.Entity<Models.DNN.File>()
     .ToTable("Files");

    builder.Entity<Models.DNN.Folder>()
     .ToTable("Folders");

    builder.Entity<Models.DNN.ListItem>()
      .ToTable("Lists")
      .HasKey(list => list.EntryId);

    builder.Entity<Models.DNN.Modules.TextHtml>()
      .ToTable("HtmlText")
      .HasKey(htmlText => htmlText.ItemId);

    builder.Entity<Models.DNN.Modules.Document>()
      .ToTable("Documents")
      .HasKey(document => document.ItemId);

    builder.Entity<Models.DNN.Modules.DocumentsSettings>()
      .ToTable("DocumentsSettings")
      .HasKey(settings => settings.ModuleId);

    builder.Entity<Models.DNN.Modules.MediaSettings>()
      .ToTable("Media")
      .HasKey(settings => settings.ModuleId);

    builder.Entity<Models.DNN.Modules.Link>()
      .ToTable("Links")
      .HasKey(document => document.ItemId);

    builder.Entity<Models.DNN.Modules.Blog>()
      .ToTable("Blog_Blogs")
      .HasKey(blog => blog.BlogId);

    builder.Entity<Models.DNN.Modules.Blog>()
      .HasMany(blog => blog.BlogEntries)
      .WithOne(entry => entry.Blog)
      .HasForeignKey("BlogID");

    builder.Entity<Models.DNN.Modules.BlogEntry>()
      .ToTable("Blog_Entries")
      .HasKey(entry => entry.EntryId);


    builder.Entity<Models.DNN.Modules.ForumGroup>()
      .ToTable("NTForums_ForumGroups")
      .HasKey(group => group.GroupId);

    builder.Entity<Models.DNN.Modules.ForumGroup>()
      .HasOne(group => group.Settings);

    builder.Entity<Models.DNN.Modules.ForumGroupSettings>()
      .ToTable("NTForums_ForumGroupSettings")
      .HasKey(settings => settings.ForumGroupID);

    builder.Entity<Models.DNN.Modules.ForumGroup>()
      .HasMany(group => group.Forums)
      .WithOne(forum => forum.ForumGroup)
      .HasForeignKey("ForumGroupId");

    builder.Entity<Models.DNN.Modules.Forum>()
      .ToTable("NTForums_Forums")
      .HasKey(forum => forum.ForumId);

    builder.Entity<Models.DNN.Modules.ForumPost>()
      .ToTable("NTForums_Posts")
      .HasKey(post => post.PostId);
  }

}
