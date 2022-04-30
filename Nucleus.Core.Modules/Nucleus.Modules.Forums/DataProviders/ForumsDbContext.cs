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
using Nucleus.Modules.Forums.Models;

namespace Nucleus.Modules.Forums.DataProviders
{
	public class ForumsDbContext : Nucleus.Data.EntityFramework.DbContext
	{
		public DbSet<Group> Groups { get; set; }
		public DbSet<Forum> Forums { get; set; }
		public DbSet<Post> Posts { get; set; }
		public DbSet<Reply> Replies { get; set; }
		public DbSet<Attachment> Attachments { get; set; }
		public DbSet<Settings> Settings { get; set; }
		public DbSet<ForumSubscription> ForumSubscriptions { get; set; }
		public DbSet<PostSubscription> PostSubscriptions { get; set; }
		public DbSet<PostTracking> PostTracking { get; set; }


		public ForumsDbContext(DbContextConfigurator<ForumsDataProvider> dbConfigurator, IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory) : base(dbConfigurator, httpContextAccessor, loggerFactory)
		{
			
		}

		/// <summary>
		/// Configure entity framework with schema information that it cannot automatically detect.
		/// </summary>
		/// <param name="builder"></param>
		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<Group>().ToTable("ForumGroups");			
			builder.Entity<Group>().Property<Guid>("ModuleId");
			builder.Entity<Group>().Ignore(group => group.Settings);

			builder.Entity<Forum>()
				.HasOne(forum => forum.Group)
				.WithMany(group => group.Forums)
				.HasForeignKey("ForumGroupId");

			builder.Entity<Forum>().Ignore(forum => forum.Settings);			
			builder.Entity<Forum>().Ignore(forum => forum.Statistics);

			builder.Entity<Post>().ToTable("ForumPosts");
			builder.Entity<Post>().Ignore(post => post.Statistics);
			builder.Entity<Post>()
				.HasOne(post => post.PostedBy)
				.WithMany()
				.HasForeignKey("AddedBy");

			builder.Entity<Post>()
				.Ignore(post => post.Tracking)
				.HasMany(post => post.Attachments)
				.WithOne()
				.HasForeignKey("ForumPostId");

			builder.Entity<Reply>().ToTable("ForumReplies");

			builder.Entity<Reply>()
				.HasOne(reply => reply.PostedBy)
				.WithMany()
				.HasForeignKey("AddedBy");

			builder.Entity<Reply>()
				.HasOne(reply => reply.Post)
				.WithMany(post => post.Replies)
				.HasForeignKey("ForumPostId");

			builder.Entity<Reply>()
				.HasMany(reply => reply.Attachments)
				.WithOne()
				.HasForeignKey("ForumReplyId");

			builder.Entity<Attachment>().ToTable("ForumAttachments");

			builder.Entity<Settings>().ToTable("ForumSettings");
			builder.Entity<Settings>().Property<Guid>("RelatedId");
			builder.Entity<Settings>().HasKey("RelatedId");

			builder.Entity<Settings>()
				.HasOne(settings => settings.StatusList)
				.WithMany()
				.HasForeignKey("StatusListId");

			builder.Entity<Permission>()
				.HasOne<Group>()
				.WithMany(group => group.Permissions)
				.HasForeignKey("RelatedId");

			builder.Entity<Permission>()
				.HasOne<Forum>()
				.WithMany(forum => forum.Permissions)
				.HasForeignKey("RelatedId");

			builder.Entity<ForumSubscription>().ToTable("ForumSubscriptions");
			builder.Entity<ForumSubscription>().HasKey( subscription => new { subscription.ForumId, subscription.UserId } );

			builder.Entity<PostSubscription>().ToTable("ForumPostSubscriptions");
			builder.Entity<PostSubscription>().HasKey(subscription => new { subscription.ForumPostId, subscription.UserId });

			builder.Entity<PostTracking>().ToTable("ForumPostTracking");
			builder.Entity<PostTracking>().HasKey(tracking => new { tracking.ForumPostId, tracking.UserId });
		}
	}
}
