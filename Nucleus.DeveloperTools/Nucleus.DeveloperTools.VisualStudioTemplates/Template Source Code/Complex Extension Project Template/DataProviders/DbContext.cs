using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Data.EntityFramework;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nucleus.Data.Common;
using Microsoft.EntityFrameworkCore;
using $nucleus_extension_namespace$.Models;

namespace $nucleus_extension_namespace$.DataProviders
{
	public class $nucleus_extension_name$DbContext : Nucleus.Data.EntityFramework.DbContext
	{
		public DbSet<$nucleus_extension_modelname$> $nucleus_extension_modelname$s { get; set; }

		public $nucleus_extension_name$DbContext(DbContextConfigurator<$nucleus_extension_name$DataProvider> dbConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory) : base(dbConfigurator, httpContextAccessor, loggerFactory)
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

			builder.Entity<$nucleus_extension_modelname$>().Property<Guid>("ModuleId");

			//builder.Entity<$nucleus_extension_name$>()
			//	.HasOne($nucleus_extension_name$ => $nucleus_extension_name$.Category)
			//	.WithMany()
			//	.HasForeignKey("CategoryId");
	}
}
}
