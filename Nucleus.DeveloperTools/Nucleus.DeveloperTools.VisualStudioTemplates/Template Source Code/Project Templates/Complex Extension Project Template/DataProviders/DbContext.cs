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
using $nucleus.extension.namespace$.Models;

namespace $nucleus.extension.namespace$.DataProviders
{
	public class $nucleus.extension.name$DbContext : Nucleus.Data.EntityFramework.DbContext
	{
		public DbSet<$nucleus.extension.model_class_name$> $nucleus.extension.model_class_name$s { get; set; }

		public $nucleus.extension.name$DbContext(DbContextConfigurator<$nucleus.extension.name$DataProvider> dbConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory) : base(dbConfigurator, httpContextAccessor, loggerFactory)
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

			builder.Entity<$nucleus.extension.model_class_name$>().Property<Guid>("ModuleId");

			//builder.Entity<$nucleus.extension.name$>()
			//	.HasOne($nucleus.extension.name$ => $nucleus.extension.name$.Category)
			//	.WithMany()
			//	.HasForeignKey("CategoryId");
	}
}
}
