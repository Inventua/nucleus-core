using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nucleus.Data.Common;
using Nucleus.Data.EntityFramework;
using Nucleus.Modules.AcceptTerms.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.AcceptTerms.DataProviders
{
  public class AcceptTermsDbContext : Nucleus.Data.EntityFramework.DbContext
  {
    public DbSet<UserAcceptedTerms> UserTermsAcceptance { get; set; }

    public AcceptTermsDbContext(DbContextConfigurator<AcceptTermsDataProvider> dbConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory) : base(dbConfigurator, httpContextAccessor, loggerFactory)
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

      builder.Entity<UserAcceptedTerms>().Property<Guid>("ModuleId");

    }
  }
}
