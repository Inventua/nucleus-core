using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Data.Common;
using Nucleus.Data.EntityFramework;
using Nucleus.OAuth.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.OAuth.Server.DataProviders
{
	public class OAuthServerDbContext : Nucleus.Data.EntityFramework.DbContext
	{
		public DbSet<ClientApp> ClientApps { get; set; }
		public DbSet<ClientAppToken> ClientAppTokens { get; set; }

		public OAuthServerDbContext(DbContextConfigurator<OAuthServerDataProvider> dbConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory) : base(dbConfigurator, httpContextAccessor, loggerFactory)
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

			builder.Entity<ClientApp>()
				.ToTable("OAuthServerClientApps")
				.HasOne<Site>();

			builder.Entity<ClientAppToken>()
				.ToTable("OAuthServerClientAppTokens");

			//builder.Entity<ClientApp>()
			//	.ToTable("OAuthServerClientApps")
			//	.HasOne<Site>()
			//	.WithMany()
			//	.HasForeignKey("SiteId").IsRequired();

			//builder.Entity<ClientApp>()
			//	.HasOne<Nucleus.Abstractions.Models.ApiKey>()
			//	.WithMany()
			//	.HasForeignKey("ApiKeyId").IsRequired();

			//builder.Entity<ClientApp>()
			//	.HasOne<Nucleus.Abstractions.Models.Page>()
			//	.WithMany()
			//	.HasForeignKey("LoginPageId").IsRequired();

			//builder.Entity<ClientAppToken>()
			//	.ToTable("OAuthServerClientAppTokens")
			//	.HasOne<ClientApp>()
			//	.WithMany()
			//	.HasForeignKey("ClientAppId").IsRequired();
		}
	}
}
