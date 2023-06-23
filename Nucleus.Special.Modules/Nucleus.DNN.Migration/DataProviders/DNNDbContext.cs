using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nucleus.Data.Common;
using Nucleus.Data.EntityFramework;
using Nucleus.DNN.Migration.Models;
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

  public DbSet<Models.DNN.File> Files { get; set; }
  public DbSet<Models.DNN.Page> Pages { get; set; }
  public DbSet<Models.DNN.User> Users { get; set; }
  

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

    builder.Entity<Models.DNN.Portal>().ToView("vw_Portals");

    //builder.Entity<DNNMigration>()
    //	.HasOne(DNNMigration => DNNMigration.Category)
    //	.WithMany()
    //	.HasForeignKey("CategoryId");
  }
}
