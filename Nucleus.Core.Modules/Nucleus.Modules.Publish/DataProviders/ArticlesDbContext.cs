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
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Publish.Models;

namespace Nucleus.Modules.Publish.DataProviders
{
	public class ArticlesDbContext : Nucleus.Data.EntityFramework.DbContext
	{
		public DbSet<Article> Articles { get; set; }
		public DbSet<Attachment> Attachments { get; set; }
		public DbSet<Category> Categories { get; set;	}

		public ArticlesDbContext(DbContextConfigurator<ArticlesDataProvider> dbConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory) : base(dbConfigurator, httpContextAccessor, loggerFactory)
		{

		}

		/// <summary>
		/// Configure entity framework with schema information that it cannot automatically detect.
		/// </summary>
		/// <param name="builder"></param>
		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<Article>().Property<Guid>("ModuleId");
			builder.Entity<Article>().Property<String>("EncodedTitle");
			//builder.Entity<Article>().Ignore(article => article.Categories);

			builder.Entity<Attachment>().ToTable("ArticleAttachments");
			
			builder.Entity<Category>().ToTable("ArticleCategories");

			//builder.Entity<Category>()
			//	.HasOne(category => category.CategoryListItem)
			//	.WithMany();

		}
	}
}
