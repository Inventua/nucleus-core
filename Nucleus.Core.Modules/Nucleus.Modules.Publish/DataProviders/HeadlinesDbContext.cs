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
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Publish.Models;

namespace Nucleus.Modules.Publish.DataProviders
{
	public class HeadlinesDbContext : Nucleus.Data.EntityFramework.DbContext
	{
		public DbSet<PublishHeadlinesFilterCategory> PublishHeadlinesFilterCategory { get; set; }

		public HeadlinesDbContext(DbContextConfigurator<HeadlinesDataProvider> dbConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory) : base(dbConfigurator, httpContextAccessor, loggerFactory)
		{

		}

		/// <summary>
		/// Configure entity framework with schema information that it cannot automatically detect.
		/// </summary>
		/// <param name="builder"></param>
		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<PublishHeadlinesFilterCategory>().ToTable ("PublishHeadlinesFilterCategories");

		}
	}
}
