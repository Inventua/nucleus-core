using System;
using Nucleus.Data.EntityFramework;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Nucleus.Modules.Documents.Models;

namespace Nucleus.Modules.Documents.DataProviders
{
	public class DocumentsDbContext : Nucleus.Data.EntityFramework.DbContext
	{
		public DbSet<Document> Documents { get; set; }

		public DocumentsDbContext(DbContextConfigurator<DocumentsDataProvider> dbConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory) : base(dbConfigurator, httpContextAccessor, loggerFactory)
		{

		}

		/// <summary>
		/// Configure entity framework with schema information that it cannot automatically detect.
		/// </summary>
		/// <param name="builder"></param>
		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<Document>().Property<Guid>("ModuleId");

			builder.Entity<Document>()
				.HasOne(document => document.Category)
				.WithMany()
				.HasForeignKey("CategoryId");

			builder.Entity<Document>()
				.HasOne(document => document.File)
				.WithMany()
				.HasForeignKey("FileId");

		}
	}
}
