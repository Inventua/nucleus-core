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
  
  public DbSet<Models.DNN.User> Users { get; set; }

  public DbSet<Models.DNN.UserRole> UserRoles { get; set; }
  public DbSet<Models.DNN.UserPortal> UserPortals { get; set; }


  public DbSet<Models.DNN.UserProfileProperty> UserProfileProperties { get; set; }
  public DbSet<Models.DNN.UserProfilePropertyDefinition> UserProfilePropertyDefinitions { get; set; } 


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

    //builder.Entity<Models.DNN.User>()
    //  .HasOne(user => user.UserPortal)
    //  .WithOne(userPortal => userPortal.User);

    //builder.Entity<Models.DNN.User>()
    //  .HasOne(user => user.UserPortal);

    builder.Entity<Models.DNN.UserProfilePropertyDefinition>()
      .ToTable("ProfilePropertyDefinition")
      .HasKey(userProfilePropertyDefinition => userProfilePropertyDefinition.PropertyDefinitionId);


    builder.Entity<Models.DNN.UserPortal>()
      .ToTable("UserPortals")
      .HasKey(userPortal => userPortal.UserPortalId);

    //builder.Entity<Models.DNN.UserPortal>()
    //  .HasOne(userPortal => userPortal.User)
    //  .WithOne(user => user.UserPortal);

    builder.Entity<Models.DNN.UserProfileProperty>()
      .ToTable("UserProfile")
      .HasKey(userProfileProperty => userProfileProperty.ProfileId);

    builder.Entity<Models.DNN.Page>()
     .ToTable("Tabs");

    //builder.Entity<DNNMigration>()
    //	.HasOne(DNNMigration => DNNMigration.Category)
    //	.WithMany()
    //	.HasForeignKey("CategoryId");
  }
}
