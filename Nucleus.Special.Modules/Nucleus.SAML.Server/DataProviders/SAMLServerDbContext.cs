using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Data.Common;
using Nucleus.Data.EntityFramework;
using Nucleus.SAML.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.SAML.Server.DataProviders
{
	public class SAMLServerDbContext : Nucleus.Data.EntityFramework.DbContext
	{
		public DbSet<ClientApp> ClientApps { get; set; }
		public DbSet<ClientAppToken> ClientAppTokens { get; set; }

		public SAMLServerDbContext(DbContextConfigurator<SAMLServerDataProvider> dbConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory) : base(dbConfigurator, httpContextAccessor, loggerFactory)
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
				.ToTable("SAMLServerClientApps")
				.HasOne<Site>();

			builder.Entity<ClientAppToken>()
				.ToTable("SAMLServerClientAppTokens");

			//builder.Entity<ClientApp>()
			//	.ToTable("SAMLServerClientApps")
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
			//	.ToTable("SAMLServerClientAppTokens")
			//	.HasOne<ClientApp>()
			//	.WithMany()
			//	.HasForeignKey("ClientAppId").IsRequired();
		}
	}
}
