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

public class DNNMigrationDbContext : Nucleus.Data.EntityFramework.DbContext
{
  public DbSet<Models.MigrationHistory> MigrationHistory { get; set; }

  public DNNMigrationDbContext(DbContextConfigurator<DNNMigrationDataProvider> dbConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory) : base(dbConfigurator, httpContextAccessor, loggerFactory)
  {

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

    //builder.Entity<Models.MigrationLog>().Property<Guid>("ModuleId");

    //builder.Entity<DNNMigration>()
    //	.HasOne(DNNMigration => DNNMigration.Category)
    //	.WithMany()
    //	.HasForeignKey("CategoryId");
  }
}
