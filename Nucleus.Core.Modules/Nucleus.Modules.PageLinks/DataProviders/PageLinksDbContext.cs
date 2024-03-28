using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Data.EntityFramework;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Data.Common;
using Microsoft.EntityFrameworkCore;
using Nucleus.Modules.PageLinks.Models;

namespace Nucleus.Modules.PageLinks.DataProviders;

public class PageLinksDbContext : Nucleus.Data.EntityFramework.DbContext
{
  public DbSet<PageLink> PageLinks { get; set; }
  //public DbSet<LinkFile> LinkFiles { get; set; }
  //public DbSet<LinkUrl> LinkUrls { get; set; }
  //public DbSet<LinkPage> LinkPages { get; set; }

  public PageLinksDbContext(DbContextConfigurator<PageLinksDataProvider> dbConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory) : base(dbConfigurator, httpContextAccessor, loggerFactory)
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

    builder.Entity<PageLink>().ToTable("PageLinks");
    builder.Entity<PageLink>().Property<Guid>("ModuleId");

    //builder.Entity<Link>()
    //  .HasOne(link => link.Category)
    //  .WithMany()
    //  .HasForeignKey("CategoryId");

    //builder.Entity<Link>().HasOne<Nucleus.Abstractions.Models.FileSystem.File>(link => link.ImageFile);

    //builder.Entity<LinkFile>().Property<Guid>("LinkId");
    //builder.Entity<LinkFile>().HasKey("LinkId");
    //builder.Entity<LinkFile>().HasOne<Nucleus.Abstractions.Models.FileSystem.File>(linkFile => linkFile.File);

    //builder.Entity<LinkUrl>().Property<Guid>("LinkId");
    //builder.Entity<LinkUrl>().HasKey("LinkId");

    //builder.Entity<LinkPage>().Property<Guid>("LinkId");
    //builder.Entity<LinkPage>().HasKey("LinkId");
    //builder.Entity<LinkPage>().HasOne<Nucleus.Abstractions.Models.Page>(linkPage => linkPage.Page);
  }
}
