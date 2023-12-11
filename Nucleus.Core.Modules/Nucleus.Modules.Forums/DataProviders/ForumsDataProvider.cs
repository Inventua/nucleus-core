using System;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Data;
using System.Security.Claims;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Modules.Forums.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Modules.Forums.DataProviders
{
	/// <summary>
	/// Forums module data provider.
	/// </summary>
	/// <remarks>
	/// This class implements the IForumsDataProvider interface, and inherits the base Nucleus entity framework data provider class.
	/// </remarks>
	public class ForumsDataProvider : Nucleus.Data.EntityFramework.DataProvider, IForumsDataProvider
	{
		protected IEventDispatcher EventManager { get; }
		protected new ForumsDbContext Context { get; }
		
		public ForumsDataProvider(ForumsDbContext context, IEventDispatcher eventManager, ILogger<ForumsDataProvider> logger) : base(context, logger)
		{
			this.EventManager = eventManager;
			this.Context = context;
		}

		#region "    Groups    "
		public async Task<Group> GetGroup(Guid id)
		{
			Group result = await this.Context.Groups
				.Where(group => group.Id == id)
				.Include(group => group.Permissions)
					.ThenInclude(permission => permission.Role)
				.Include(group => group.Permissions)
					.ThenInclude(permission => permission.PermissionType)
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();

			if (result != null)
			{
				result.Settings = await GetSettings(result.Id);
			}

			return result;
		}

		public async Task<IEnumerable<Group>> ListGroups(PageModule pageModule)
		{
			IEnumerable<Group> results = await this.Context.Groups
				.Where(group => EF.Property<Guid>(group, "ModuleId") == pageModule.Id)
				.Include(group => group.Permissions)
					.ThenInclude(permission => permission.Role)
				.Include(group => group.Permissions)
					.ThenInclude(permission => permission.PermissionType)
				.OrderBy(group => group.SortOrder)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();

			foreach (Group result in results)
			{
				result.Settings = await GetSettings(result.Id);
			}

			return results;
		}


		public async Task SaveGroup(PageModule module, Group group)
		{
			Action raiseEvent;

			// We have to store/NULL/restore the permissions collection, otherwise EF tries to update it, and we want to handle saving permissions
			// in .SavePermissions
			List<Permission> permissions = group.Permissions;
			group.Permissions = null;

			//Boolean isNew = !this.Context.Groups.Where(existing => existing.Id == group.Id).AsNoTracking().Any();
			Group existing = await this.Context.Groups
				.Where(existing => existing.Id == group.Id)
				.FirstOrDefaultAsync();

			if (existing == null)
			{
				// new group record
				this.Context.Add(group);
				this.Context.Entry(group).Property("ModuleId").CurrentValue = module.Id;
				group.SortOrder = await GetTopForumGroupSortOrder(module.Id) + 10;
				this.Context.Entry(group).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Group, Create>(group); });
			}
			else
			{
				// existing record
				this.Context.Entry(existing).CurrentValues.SetValues(group);
				this.Context.Entry(existing).Property("ModuleId").CurrentValue = module.Id;
				this.Context.Entry(existing).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Group, Update>(existing); });
			}

			await this.Context.SaveChangesAsync<Group>();
			
			await SaveSettings(group.Id, group.Settings);

			group.Permissions = permissions;

			raiseEvent.Invoke();
		}

		public async Task DeleteGroup(Group group)
		{
			if (await this.Context.Forums.Where(forum => forum.Group.Id == group.Id).AnyAsync())
			{
				throw new InvalidOperationException("Cannot delete a forum group with one or more forums.");
			}

			Settings settings = this.Context.Settings.Where(setting => EF.Property<Guid>(setting, "RelatedId") == group.Id).FirstOrDefault();

			if (settings != null)
			{
				this.Context.Entry(settings).State = EntityState.Deleted;
			}

			this.Context.Remove(group);
			await this.Context.SaveChangesAsync();
		}

		private async Task<int> GetTopForumGroupSortOrder(Guid moduleId)
		{
			Group group = await this.Context.Groups
				.Where(group => EF.Property<Guid>(group, "ModuleId") == moduleId)
				.OrderByDescending(group => group.SortOrder)
				.AsNoTracking()
				.FirstOrDefaultAsync();

			return group == null ? 10 : group.SortOrder;

		}

		#endregion

		#region "    Forums    "
		public async Task<Forum> GetForum(Guid id)
		{
			Forum result = await this.Context.Forums
				.Where(forum => forum.Id == id)
				.Include(forum => forum.Group)
					.ThenInclude(group => group.Permissions)
						.ThenInclude(permission => permission.Role)
				.Include(forum => forum.Group)
					.ThenInclude(group => group.Permissions)
						.ThenInclude(permission => permission.PermissionType)
				.Include(forum => forum.Group)
				.Include(forum => forum.Permissions)
					.ThenInclude(permission => permission.Role)
				.Include(forum => forum.Permissions)
					.ThenInclude(permission => permission.PermissionType)
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();

			if (result != null)
			{
				result.Settings = await GetSettings(id);
				result.Statistics = await GetForumStatistics(id);

				result.Group.Settings = await GetSettings(result.Group.Id);
			}

			return result;
		}

		private async Task<ForumStatistics> GetForumStatistics(Guid forumId)
		{
			ForumStatistics result = new();
			
			result.PostCount = await this.Context.Posts
				.Where(post => post.ForumId == forumId)
				.AsNoTracking()
				.CountAsync();

			result.ReplyCount = await this.Context.Posts
				.Where(post => post.ForumId == forumId)
				.SelectMany(post => post.Replies)
				.AsNoTracking()
				.CountAsync();

			result.LastPost = await GetLastPost(forumId);
			if (result.LastPost != null)
			{
				result.LastReply = await GetLastReply(result.LastPost.Id);
			}

			return result;
		}

		private async Task<Post> GetLastPost(Guid forumId)
		{
			return await this.Context.Posts
				.Where(post => post.ForumId == forumId)
				.Include(post => post.PostedBy)				
				.OrderByDescending(post => post.DateAdded)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<List<Guid>> ListForums(Group group)
		{
			return await this.Context.Forums
				.Where(forum => forum.Group.Id == group.Id)
				.OrderBy(forum => forum.SortOrder)
				.AsNoTracking()
				.Select(forum => forum.Id)
				.ToListAsync();

			////   IList<Forum> results = await this.Context.Forums
			////	.Where(forum => forum.Group.Id == group.Id)
			////	.Include(forum => forum.Group)
			////		.ThenInclude(group => group.Permissions)
			////			.ThenInclude(permission => permission.Role)
			////	.Include(forum => forum.Group)
			////		.ThenInclude(group => group.Permissions)
			////			.ThenInclude(permission => permission.PermissionType)
			////	.Include(forum => forum.Group)
			////	.OrderBy(forum => forum.SortOrder)
			////     .AsNoTracking()
			////     .ToListAsync();

			////foreach (Forum result in results)
			////{
			////	result.Settings = await GetSettings(result.Id);
			////	result.Statistics = await GetForumStatistics(result.Id);

			////	result.Group.Settings = await GetSettings(result.Group.Id);
			////}

			////return results;
		}

		public async Task SaveForum(Group group, Forum forum)
		{
			Action raiseEvent;
			
			// We have to store/NULL/restore the permissions collection, otherwise EF tries to update it, and we want to handle saving permissions
			// in .SavePermissions
			List<Permission> permissions = forum.Permissions;
			forum.Permissions = null;

			//Boolean isNew = !this.Context.Forums.Where(existing => existing.Id == forum.Id).AsNoTracking().Any();
			Forum existing = await this.Context.Forums
				.Where(existing => existing.Id == forum.Id)
				.FirstOrDefaultAsync();

			//this.Context.Attach(forum);

			if (existing == null)
			{
				this.Context.Add(forum);
				this.Context.Entry(forum).Property("ForumGroupId").CurrentValue = group.Id;
				forum.SortOrder = await GetTopForumSortOrder(group.Id) + 10;
				this.Context.Entry(forum).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Forum, Create>(forum); });
			}
			else
			{
				this.Context.Entry(existing).CurrentValues.SetValues(forum);
				this.Context.Entry(existing).Property("ForumGroupId").CurrentValue = group.Id;
				this.Context.Entry(existing).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Forum, Update>(existing); });
			}

			await this.Context.SaveChangesAsync<Forum>();

			if (forum.Settings != null)
			{
				// save settings
				await SaveSettings(forum.Id, forum.Settings);
			}

			forum.Permissions = permissions;

			raiseEvent.Invoke();
		}

		public async Task DeleteForum(Forum forum)
		{
			if (await this.Context.Posts.Where(post => post.ForumId == forum.Id).AnyAsync())
			{
				throw new InvalidOperationException("Cannot delete a forum with posts.  Mark the forum as disabled instead.");
			}

			Settings settings = this.Context.Settings.Where(setting => EF.Property<Guid>(setting, "RelatedId") == forum.Id).FirstOrDefault();
			
			if (settings != null)
			{
				this.Context.Entry(settings).State = EntityState.Deleted;
			}

			await this.Context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ForumSubscriptions WHERE ForumId={forum.Id}");

			Forum existing = await this.Context.Forums
				.Where(existing => existing.Id == forum.Id)
				.FirstOrDefaultAsync();

			if (existing != null)
			{
				this.Context.Remove(existing);
			}
			else
			{
				this.Context.Remove(forum);
			}

			await this.Context.SaveChangesAsync();
		}

		private async Task<int> GetTopForumSortOrder(Guid groupId)
		{
			Forum forum = await this.Context.Forums
				.Where(forum => forum.Group.Id == groupId)
				.OrderByDescending(forum => forum.SortOrder)
				.AsNoTracking()
				.FirstOrDefaultAsync();

			return forum == null ? 10 : forum.SortOrder;
		}

		#endregion

		#region "    Settings    "
		public async Task<Settings> GetSettings(Guid relatedId)
		{
			return await this.Context.Settings
				.Where(settings => EF.Property<Guid>(settings, "RelatedId") == relatedId)
				.Include(settings => settings.StatusList)
					.ThenInclude(statuslist => statuslist.Items)
				.Include(forum => forum.AttachmentsFolder)
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task SaveSettings(Guid relatedId, Settings settings)
		{
			Action raiseEvent;

			Boolean isNew = !this.Context.Settings.Where(existing => EF.Property<Guid>(existing, "RelatedId") == relatedId).Any();

			this.Context.Attach(settings);
			this.Context.Entry(settings).Property("RelatedId").CurrentValue = relatedId;

			if (isNew)
			{
				this.Context.Entry(settings).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Settings, Create>(settings); });
			}
			else
			{
				this.Context.Entry(settings).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Settings, Update>(settings); });
			}

			await this.Context.SaveChangesAsync<Settings>();

			raiseEvent.Invoke();
		}

		#endregion

		#region "    Posts    "
		public async Task<Post> GetForumPost(Guid id)
		{
			Post result = await this.Context.Posts
				.Where(post => post.Id == id)
				.Include(post => post.Status)
				.Include(post => post.PostedBy)
				.Include(post => post.Replies)
					.ThenInclude(reply => reply.Attachments)
						.ThenInclude(attachment => attachment.File)
				.Include(post => post.Replies)
					.ThenInclude(reply => reply.ReplyTo)
				.Include(post => post.Replies)
					.ThenInclude(reply => reply.PostedBy)
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();

			if (result != null)
			{
				// Entity-framework can't handle the logic for post attachments, which belong directly to the post only
				// if ForumReplyId is null so we have to read "manually"
				result.Attachments = this.Context.Attachments
					.Where(attachment => EF.Property<Guid>(attachment, "ForumPostId") == result.Id && EF.Property<Guid?>(attachment, "ForumReplyId") == null)
						.Include(attachment => attachment.File)
						.AsNoTracking()
						.ToList();
			
				result.Statistics = await GetPostStatistics(id, FlagStates.IsAny);
			}

			return result;
		}

		public async Task<Post> FindForumPost(Guid forumId, string subject)
		{
			Post result = await this.Context.Posts
				.Where(post => post.ForumId == forumId && post.Subject == subject)
				.Include(post => post.Status)
				.Include(post => post.PostedBy)
				.Include(post => post.Replies)
					.ThenInclude(reply => reply.Attachments)
						.ThenInclude(attachment => attachment.File)
				.Include(post => post.Replies)
					.ThenInclude(reply => reply.ReplyTo)
				.Include(post => post.Replies)
					.ThenInclude(reply => reply.PostedBy)
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();

			if (result != null)
			{
				// Entity-framework can't handle the logic for post attachments, which belong directly to the post only
				// if ForumReplyId is null so we have to read "manually"
				result.Attachments = this.Context.Attachments
					.Where(attachment => EF.Property<Guid>(attachment, "ForumPostId") == result.Id && EF.Property<Guid?>(attachment, "ForumReplyId") == null)
						.Include(attachment => attachment.File)
						.AsNoTracking()
						.ToList();

				result.Statistics = await GetPostStatistics(result.Id, FlagStates.IsAny);
			}

			return result;
		}


		public async Task<IList<Post>> ListForumPosts(Forum forum, FlagStates approved)
		{			
			return await this.Context.Posts
				.Where(post => post.ForumId == forum.Id && (approved == FlagStates.IsAny || (post.IsApproved == (approved == FlagStates.IsTrue))))
				.Include(post => post.Status)
				.Include(post => post.Attachments)
					.ThenInclude(attachment => attachment.File)
				.Include(post => post.PostedBy)
				.OrderByDescending(post => post.IsPinned)
				.ThenByDescending(post => post.DateAdded)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<Post>> ListForumPosts(Forum forum, ClaimsPrincipal user, Nucleus.Abstractions.Models.Paging.PagingSettings settings, FlagStates approved, string sortKey, Boolean descending)
		{
			Nucleus.Abstractions.Models.Paging.PagedResult<Post> results = new(settings);

			IQueryable<Post> query = this.Context.Posts
				.Where(post => post.ForumId == forum.Id && (post.AddedBy == user.GetUserId() || (approved == FlagStates.IsAny || (post.IsApproved == (approved == FlagStates.IsTrue)))))
				.Include(post => post.Status)
				.Include(post => post.Attachments)
					.ThenInclude(attachment => attachment.File)
				.Include(post => post.PostedBy);

			results.TotalCount = await query
				.AsNoTracking()
				.CountAsync();

			switch (sortKey.ToLower())
			{
				default: 
					query = query
						.OrderByDescending(post => post.IsPinned)
						.ThenByDescending(post => post.DateAdded);
					break;
			}

			IList<Post> posts = await query
				.Skip(settings.FirstRowIndex)
				.Take(settings.PageSize)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();

			foreach (Post post in posts)
			{
				post.Statistics = await GetPostStatistics(post.Id, approved);
			}

			results.Items = posts;

			return results;
		}

		public async Task DeleteForumPost(Post post)
		{
			await this.Context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ForumPostSubscriptions WHERE ForumPostId={post.Id}");

			this.Context.Posts.Remove(post);
			await this.Context.SaveChangesAsync<Post>();
		}

		public async Task SaveForumPost(Forum forum, Post post)
		{
			Action raiseEvent;
			
			Boolean isNew = !this.Context.Posts
				.Where(existing => existing.Id == post.Id)
				.AsNoTracking()
				.Any();
			
			this.Context.Attach(post);
			this.Context.Entry(post).Property("ForumId").CurrentValue = forum.Id;
			if (isNew)
			{
				this.Context.Entry(post).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Post, Create>(post); });
			}
			else
			{
				this.Context.Entry(post).State = EntityState.Modified;
				
				// These properties can only be set for new records, or by calling the SetForumPostApproved/Locked/Pinned/Status functions
				this.Context.Entry(post).Property(post => post.IsApproved).IsModified = false;
				this.Context.Entry(post).Property(post => post.IsRejected).IsModified = false;
				this.Context.Entry(post).Property(post => post.IsLocked).IsModified = false;
				this.Context.Entry(post).Property(post => post.IsPinned).IsModified = false;
				this.Context.Entry(post).Reference(post => post.Status).IsModified = false;

				// Forum id can't be changed for an existing post (if we implement a "move" function in the future, it will use a special function)
				this.Context.Entry(post).Property(post => post.ForumId).IsModified = false;

				raiseEvent = new(() => { this.EventManager.RaiseEvent<Post, Update>(post); });
			}

			await this .Context.SaveChangesAsync<Post>();

			raiseEvent.Invoke();
		}

		public async Task SaveAttachments(Guid postId, Guid? replyId, IEnumerable<Attachment> attachments, IEnumerable<Attachment> originalAttachments)
		{
			if (attachments == null) return;

			// delete removed attachments
			if (originalAttachments != null)
			{
				foreach (Attachment original in originalAttachments)
				{
					if (!attachments.Where(attachment => attachment.Id == original.Id || attachment?.File.Id == original.File?.Id).Any())
					{
						// delete the attachment
						this.Context.Attachments.Remove(original);
					}
				}
			}

			// create added attachments
			foreach (Attachment attachment in attachments)
			{
				Boolean found = false;

				if (originalAttachments != null)
				{
					foreach (Attachment originalAttachment in originalAttachments)
					{
						if (attachment.Id == originalAttachment.Id || attachment.File?.Id == originalAttachment.File?.Id)
						{
							found = true;
							break;
						}
					}
				}

				if (found)
				{
					// attachments are never updated
				}
				else
				{
					this.Context.Attachments.Add(attachment);
					this.Context.Entry(attachment).State = EntityState.Added;
					this.Context.Entry(attachment).Property("ForumPostId").CurrentValue = postId;
					this.Context.Entry(attachment).Property("ForumReplyId").CurrentValue = replyId;					
				}
			}

			await this.Context.SaveChangesAsync<Attachment>();
		}
		
		public async Task DeletePostAttachments(Post post)
		{
			IEnumerable<Attachment> originalAttachments = this.Context.Attachments
				.Where(attachment => EF.Property<Guid>(attachment, "ForumPostId") == post.Id)
				.AsNoTracking();

			if (originalAttachments == null) return;

			// delete attachments
			foreach (Attachment original in originalAttachments)
			{
				// delete the attachment
				this.Context.Attachments.Remove(original);

				await this.Context.SaveChangesAsync<Attachment>();
			}
		}

		public async Task DeleteReplyAttachments(Reply reply)
		{
			IEnumerable<Attachment> originalAttachments = this.Context.Attachments
				.Where(attachment => EF.Property<Guid>(attachment, "ForumReplyId") == reply.Id)
				.AsNoTracking();

			if (originalAttachments == null) return;

			// delete attachments
			foreach (Attachment original in originalAttachments)
			{
				// delete the attachment
				this.Context.Attachments.Remove(original);

				await this.Context.SaveChangesAsync<Attachment>();
			}
		}



		public async Task SetForumPostPinned(Post post, Boolean value)
		{
			Post existing = await this.Context.Posts
				.Where(existing => existing.Id == post.Id)
				.FirstOrDefaultAsync();

			if (existing != null)
			{
				post.IsPinned = value;
				this.Context.Entry(existing).Property(existing => existing.IsPinned).CurrentValue = value;
				await this.Context.SaveChangesAsync<Post>();
			}
			
		}

		public async Task SetForumPostLocked(Post post, Boolean value)
		{
			Post existing = await this.Context.Posts
				.Where(existing => existing.Id == post.Id)
				.FirstOrDefaultAsync();

			if (existing != null)
			{
				existing.IsLocked = value;
				this.Context.Entry(existing).Property(existing => existing.IsLocked).CurrentValue = value;
				await this.Context.SaveChangesAsync<Post>();
			}
		}

		public async Task SetForumPostStatus(Post post, Nucleus.Abstractions.Models.ListItem value)
		{
			Post existing = await this.Context.Posts
				.Where(existing => existing.Id == post.Id)
				.FirstOrDefaultAsync();

			if (existing != null)
			{
				existing.Status = value;
				this.Context.Entry(existing).Reference(existing => existing.Status).CurrentValue = value;
				await this.Context.SaveChangesAsync<Post>();
			}
		}

		public async Task SetForumPostApproved(Post post, Boolean value)
		{
			Post existing = await this.Context.Posts
				.Where(existing => existing.Id == post.Id)
				.FirstOrDefaultAsync();

			if (existing != null)
			{
				post.IsApproved = value;
				if (value)
				{
					this.Context.Entry(existing).Property(existing => existing.IsRejected).CurrentValue = false ;
				}
				this.Context.Entry(existing).Property(existing => existing.IsApproved).CurrentValue = value;
				await this.Context.SaveChangesAsync<Post>();

				this.EventManager.RaiseEvent<Post, Approved>(post);
			}
		}

		public async Task SetForumPostRejected(Post post, Boolean value)
		{
			Post existing = await this.Context.Posts
				.Where(existing => existing.Id == post.Id)
				.FirstOrDefaultAsync();

			if (existing != null)
			{
				post.IsRejected = value;
				if (value)
				{
					this.Context.Entry(existing).Property(existing => existing.IsApproved).CurrentValue = false;					
				}
				this.Context.Entry(existing).Property(existing => existing.IsRejected).CurrentValue = value;
				await this.Context.SaveChangesAsync<Post>();

				this.EventManager.RaiseEvent<Post, Rejected>(post);
			}
		}

		private async Task<PostStatistics> GetPostStatistics(Guid postId, FlagStates approved)
		{
			PostStatistics result = new();

			result.ReplyCount = await this.Context.Replies
				.Where(reply => reply.Post.Id == postId && (approved == FlagStates.IsAny || (reply.IsApproved == (approved == FlagStates.IsTrue))))
				.CountAsync();

			result.LastReply = await GetLastReply(postId);
			
			return result;	
		}

		public async Task<List<Attachment>> ListPostAttachments(Guid postId)
		{
			return await this.Context.Attachments
				.Where(attachment => EF.Property<Guid>(attachment,"ForumPostId") == postId && EF.Property<Guid?>(attachment, "ForumReplyId") == null)
				.Include(attachment => attachment.File)
				.AsNoTracking()
				.ToListAsync();			
		}

		public async Task<List<Attachment>> ListReplyAttachments(Guid postId, Guid replyId)
		{
			return await this.Context.Attachments
				.Where(attachment => EF.Property<Guid>(attachment, "ForumPostId") == postId && EF.Property<Guid?>(attachment, "ForumReplyId") == replyId)
				.Include(attachment => attachment.File)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Reply> GetForumPostReply(Guid id)
		{
			return await this.Context.Replies
				.Where(reply => reply.Id == id)
				.Include(reply => reply.Attachments)
					.ThenInclude(attachment => attachment.File)
				.Include(reply => reply.Post)
				.Include(reply => reply.ReplyTo)
				.Include(reply => reply.PostedBy)
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<IList<Reply>> ListForumPostReplies(Post post, FlagStates approved)
		{
			return await this.Context.Replies
				.Where(reply => reply.Post.Id == post.Id && (approved == FlagStates.IsAny || (reply.IsApproved == (approved == FlagStates.IsTrue))))
				.Include(reply => reply.Attachments)
					.ThenInclude(attachment => attachment.File)
				.Include(reply => reply.Post)
				.Include(reply => reply.ReplyTo)
				.Include(reply => reply.PostedBy)
				.OrderBy(reply => reply.DateAdded)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<IList<Reply>> ListForumPostReplies(Post post, ClaimsPrincipal user, FlagStates approved)
		{
			return await this.Context.Replies
				.Where(reply => reply.Post.Id == post.Id && (reply.AddedBy == user.GetUserId() || (approved == FlagStates.IsAny || (reply.IsApproved == (approved == FlagStates.IsTrue)))))
				.Include(reply => reply.Attachments)
					.ThenInclude(attachment => attachment.File)
				.Include(reply => reply.Post)
				.Include(reply => reply.ReplyTo)
				.Include(reply => reply.PostedBy)
				.OrderBy(reply => reply.DateAdded)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task DeleteForumPostReply(Reply reply)
		{
			this.Context.Replies.Remove(reply);
			await this.Context.SaveChangesAsync<Reply>();
		}

		public async Task SaveForumPostReply(Post post, Reply reply)
		{
			Action raiseEvent;

			Boolean isNew = !this.Context.Replies.Where(existing => existing.Id == reply.Id).Any();

			this.Context.Attach<Reply>(reply);
			this.Context.Entry(reply).Property("ForumPostId").CurrentValue = post.Id;

			if (isNew)
			{
				this.Context.Entry(reply).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Reply, Create>(reply); });
			}
			else
			{
				this.Context.Entry(reply).State = EntityState.Modified;

				// These properties can only be set for new records, or by calling the SetForumPostApproved/Locked/Pinned/Status functions
				this.Context.Entry(reply).Property(reply => reply.IsApproved).IsModified = false;
				this.Context.Entry(reply).Property(reply => reply.IsRejected).IsModified = false;

				raiseEvent = new(() => { this.EventManager.RaiseEvent<Reply, Update>(reply); });
			}

			await this.Context.SaveChangesAsync<Reply>();

			raiseEvent.Invoke();
		}


		public async Task SetForumPostReplyApproved(Reply reply, Boolean value)
		{
			Reply existing = await this.Context.Replies
				.Where(existing => existing.Id == reply.Id)
				.FirstOrDefaultAsync();

			if (existing != null)
			{
				reply.IsApproved = value;
				if (value)
				{
					this.Context.Entry(existing).Property(existing => existing.IsRejected).CurrentValue = false;
				}
				this.Context.Entry(existing).Property(existing => existing.IsApproved).CurrentValue = value;
				await this.Context.SaveChangesAsync<Reply>();

				this.EventManager.RaiseEvent<Reply, Approved>(reply);
			}
		}

		public async Task SetForumPostReplyRejected(Reply reply, Boolean value)
		{
			Reply existing = await this.Context.Replies
				.Where(existing => existing.Id == reply.Id)
				.FirstOrDefaultAsync();

			if (existing != null)
			{
				reply.IsRejected = value;
				if (value)
				{
					this.Context.Entry(existing).Property(existing => existing.IsApproved).CurrentValue = false;
				}
				this.Context.Entry(existing).Property(existing => existing.IsRejected).CurrentValue = value;
				await this.Context.SaveChangesAsync<Reply>();

				this.EventManager.RaiseEvent<Reply, Rejected>(reply);
			}
		}

		private async Task<Reply> GetLastReply(Guid postId)
		{
			return await this.Context.Replies
				.Where(reply => reply.Post.Id == postId)
				.Include(reply => reply.Post)
				.Include(reply => reply.ReplyTo)
				.Include(reply => reply.PostedBy)
				.OrderByDescending(reply => reply.DateAdded)
				.AsSplitQuery()
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		#endregion

		#region "    Subscriptions    "
		public async Task SubscribeForumGroup(Guid groupId, User user)
		{
			Boolean alreadyExists = await this.Context.ForumGroupSubscriptions
				.Where(subscription => subscription.ForumGroupId == groupId && subscription.User.Id == user.Id)
				.AsNoTracking()
				.AnyAsync();

			if (!alreadyExists)
			{
				this.Context.Add(new ForumGroupSubscription() { ForumGroupId = groupId, User = new() { Id = user.Id } });
				await this.Context.SaveChangesAsync<ForumGroupSubscription>();
			}
		}

		public async Task UpdateForumGroupSubscriptionNotificationFrequency(Guid groupId, User user, NotificationFrequency frequency)
		{
			ForumGroupSubscription subscription = await this.Context.ForumGroupSubscriptions
				.Where(subscription => subscription.ForumGroupId == groupId && subscription.User.Id == user.Id)
				.FirstOrDefaultAsync();

			if (subscription != null)
			{
				subscription.NotificationFrequency = frequency;
				await this.Context.SaveChangesAsync<ForumGroupSubscription>();
			}
		}

		public async Task UnSubscribeForumGroup(Guid groupId, Guid userId)
		{
			Group group = await this.Context.Groups
				.Where(group => group.Id == groupId)
				.Include(group => group.Forums)
				.AsNoTracking()
				.AsSplitQuery()
				.FirstOrDefaultAsync();
			
			ForumGroupSubscription subscription = await this.Context.ForumGroupSubscriptions
				.Where(subscription => subscription.ForumGroupId == groupId && subscription.User.Id == userId)
				.FirstOrDefaultAsync();

			IEnumerable<MailQueue> mailQueue = await this.Context.MailQueue
				.Where(queue => queue.UserId == userId && group.Forums.Select(forum=>forum.Id).Contains(queue.Post.ForumId))
				.ToListAsync();

			if (subscription != null)
			{
				this.Context.Remove(subscription);
				this.Context.RemoveRange(mailQueue);

				await this.Context.SaveChangesAsync();
			}
		}

		public async Task<List<ForumGroupSubscription>> ListForumGroupSubscribers(Guid groupId)
		{
			return await this.Context.ForumGroupSubscriptions
				.Where(subscription => subscription.ForumGroupId == groupId)
				.Include(subscription => subscription.User)
					.ThenInclude(user => user.Profile)
						.ThenInclude(profilevalue => profilevalue.UserProfileProperty)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();
		}
		
		public async Task SubscribeForum(Guid forumId, User user)
		{
			Boolean alreadyExists = await this.Context.ForumSubscriptions
				.Where(subscription => subscription.ForumId == forumId && subscription.User.Id == user.Id)
				.AsNoTracking()
				.AnyAsync();

			if (!alreadyExists)
			{
				this.Context.Add(new ForumSubscription() { ForumId = forumId, User = new() { Id = user.Id } });
				await this.Context.SaveChangesAsync<ForumSubscription>();
			}
		}

		public async Task UpdateForumSubscriptionNotificationFrequency(Guid forumId, User user, NotificationFrequency frequency)
		{
			ForumSubscription subscription = await this.Context.ForumSubscriptions
				.Where(subscription => subscription.ForumId == forumId && subscription.User.Id == user.Id)
				.FirstOrDefaultAsync();

			if (subscription != null)
			{
				subscription.NotificationFrequency = frequency;
				await this.Context.SaveChangesAsync<ForumSubscription>();
			}
		}

		public async Task UnSubscribeForum(Guid forumId, Guid userId)
		{
			ForumSubscription subscription = await this.Context.ForumSubscriptions
				.Where(subscription => subscription.ForumId == forumId && subscription.User.Id == userId)
				.FirstOrDefaultAsync();

			IEnumerable<MailQueue> mailQueue = await this.Context.MailQueue
				.Where(queue => queue.UserId == userId && queue.Post.ForumId == forumId)
				.ToListAsync();

			if (subscription != null)
			{
				this.Context.Remove(subscription);
				this.Context.RemoveRange(mailQueue);

				await this.Context.SaveChangesAsync();
			}			
		}
				
		public async Task<List<ForumSubscription>> ListForumSubscribers(Guid forumId)
		{
			return await this.Context.ForumSubscriptions
				.Where(subscription => subscription.ForumId == forumId)
				.Include(subscription => subscription.User)
					.ThenInclude(user => user.Profile)
						.ThenInclude(profilevalue => profilevalue.UserProfileProperty)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<ForumGroupSubscription> GetForumGroupSubscription(Guid groupId, Guid userId)
		{
			return await this.Context.ForumGroupSubscriptions
				.Where(subscription => subscription.ForumGroupId == groupId && subscription.User.Id == userId)
				.Include(subscription => subscription.User)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<ForumSubscription> GetForumSubscription(Guid forumId, Guid userId)
		{
			return await this.Context.ForumSubscriptions
				.Where(subscription => subscription.ForumId == forumId && subscription.User.Id == userId)
				.Include(subscription => subscription.User)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task SubscribeForumPost(Guid postId, User user)
		{
			Boolean alreadyExists = await this.Context.PostSubscriptions
				.Where(subscription => subscription.ForumPostId == postId && subscription.User.Id == user.Id)
				.AsNoTracking()
				.AnyAsync();

			if (!alreadyExists)
			{
				this.Context.Add(new PostSubscription() { ForumPostId = postId, User = user });
				await this.Context.SaveChangesAsync<PostSubscription>();
			}
		}

		public async Task UnSubscribeForumPost(Guid postId, User user)
		{
			PostSubscription subscription = await this.Context.PostSubscriptions
				.Where(subscription => subscription.ForumPostId == postId && subscription.User.Id == user.Id)
				.AsNoTracking()
				.FirstOrDefaultAsync();

			if (subscription != null)
			{
				this.Context.Remove(subscription);
			}

			await this.Context.SaveChangesAsync<PostSubscription>();
		}

		public async Task<List<PostSubscription>> ListPostSubscribers(Guid postId)
		{
			return await this.Context.PostSubscriptions
				.Where(subscription => subscription.ForumPostId == postId)
				.Include(subscription => subscription.User)
					.ThenInclude(user => user.Profile)
						.ThenInclude(profilevalue => profilevalue.UserProfileProperty)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<PostSubscription> GetPostSubscription(Guid postId, Guid userId)
		{
			return await this.Context.PostSubscriptions
				.Where(subscription => subscription.ForumPostId == postId && subscription.User.Id == userId)
				.Include(subscription => subscription.User)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}
		#endregion

		#region "    Post Tracking    "
		public async Task<PostTracking> GetPostTracking(Guid postId, Guid userId) 
		{
			return await this.Context.PostTracking
				.Where(tracking => tracking.ForumPostId == postId && tracking.UserId == userId)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}
		
		public async Task SavePostTracking(Guid postId, Guid userId)
		{
			PostTracking tracking = await this.Context.PostTracking
				.Where(tracking => tracking.ForumPostId == postId && tracking.UserId == userId)
				.FirstOrDefaultAsync();

			if (tracking == null)
			{
				this.Context.Add(new PostTracking() { ForumPostId = postId, UserId = userId });
			}
			else
			{
				this.Context.Entry(tracking).State = EntityState.Modified;
			}

			await this.Context.SaveChangesAsync<PostTracking>();
		}

		public async Task DeletePostTracking(Guid postId, Guid userId)
		{
			PostTracking tracking = await this.Context.PostTracking
				.Where(tracking => tracking.ForumPostId == postId && tracking.UserId == userId)
				.FirstOrDefaultAsync();

			if (tracking != null)
			{
				this.Context.Remove(tracking);
			}

			await this.Context.SaveChangesAsync<PostTracking>();
		}
		#endregion

		#region "    Mail Queue    "

		public async Task<Boolean> IsQueued(MailQueue mailQueue)
		{
			return await this.Context.MailQueue
				.Where(existing => existing.UserId == mailQueue.UserId && existing.MailTemplateId == mailQueue.MailTemplateId && existing.Post.Id == mailQueue.Post.Id && ((mailQueue.Reply == null && existing.Reply == null) || (mailQueue.Reply != null && existing.Reply.Id == mailQueue.Reply.Id)))
				.AsNoTracking()
				.AnyAsync();
		}

		public async Task SaveMailQueue(MailQueue mailQueue)
		{
			MailQueue existing = await this.Context.MailQueue
				.Where(existing => 
					existing.Id == mailQueue.Id || 
					(
						existing.UserId == mailQueue.UserId && 
						existing.Post.Id == mailQueue.Post.Id &&
						existing.MailTemplateId == mailQueue.MailTemplateId && 
						((mailQueue.Reply == null && existing.Reply == null) || (mailQueue.Reply != null && existing.Reply.Id == mailQueue.Reply.Id))
					))
				.FirstOrDefaultAsync();

			if (existing == null)
			{
				this.Context.Add(mailQueue);
			}
			else
			{
				this.Context.Entry(existing).CurrentValues.SetValues(mailQueue);
			}

			await this.Context.SaveChangesAsync<MailQueue>();
		}

		public async Task SetMailQueueStatus(MailQueue mailQueue)
		{
			MailQueue existing = await this.Context.MailQueue
				.Where(existing => existing.Id == mailQueue.Id)
				.FirstOrDefaultAsync();

			if (existing != null)
			{
				this.Context.Entry(existing).Property(mailQueue=>mailQueue.Status).CurrentValue = mailQueue.Status;
				this.Context.Entry(existing).State = EntityState.Modified;
			}

			await this.Context.SaveChangesAsync<MailQueue>();
		}

		public async Task DeleteMailQueue(MailQueue mailQueue)
		{
			MailQueue item = await this.Context.MailQueue
				.Where(item => item.Id == mailQueue.Id)
				.FirstOrDefaultAsync();

			if (item != null)
			{
				this.Context.Remove(item);
			}

			await this.Context.SaveChangesAsync<MailQueue>();
		}

		/// <summary>
		/// Return a list of queued mail queue items.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// The list is sorted by user, module, post and reply for performance, because this function is called by the email sending scheduled task, which groups notifications 
		/// for a user into a single email.
		/// </remarks>

		public async Task<IList<MailQueue>> ListMailQueue(NotificationFrequency frequency)
		{
			return await this.Context.MailQueue
				.Where(item => item.Status == MailQueue.MailQueueStatus.Queued && item.NotificationFrequency == frequency)
				.Include(item => item.Post)
					.ThenInclude(post => post.Status)
				.Include(item => item.Post)
					.ThenInclude(post => post.PostedBy)

				.Include(item => item.Reply)
					.IgnoreAutoIncludes()
				.Include(item => item.Reply)
					.ThenInclude(reply => reply.ReplyTo)
				.Include(item => item.Reply)
					.ThenInclude(reply => reply.PostedBy)
				.Include(item => item.Reply)
					.ThenInclude(reply => reply.Post)          
						.ThenInclude(post => post.PostedBy)
				 .Include(item => item.Reply)
					.ThenInclude(reply => reply.Post)
						.ThenInclude(post => post.Status)

				.OrderBy(item => item.UserId)
					.ThenBy(item => item.ModuleId)
					.ThenBy(item => item.Post.ForumId)
					.ThenBy(item => item.MailTemplateId )
					.ThenBy(item => item.Post.Id)
					.ThenBy(item => item.Reply.Id)
					.ThenBy(item => item.DateAdded)
				.AsSplitQuery()
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task TruncateMailQueue(TimeSpan sentBefore)
		{
			DateTime beforeDate = DateTime.Now.Add(-sentBefore);

			await this.Context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ForumMailQueue WHERE Status = {MailQueue.MailQueueStatus.Sent} AND DateChanged < {beforeDate}");

			////   IEnumerable<MailQueue> items = await this.Context.MailQueue
			////	.Where(item => item.Status == MailQueue.MailQueueStatus.Sent && item.DateChanged < beforeDate)
			////	.ToListAsync();

			////if (items.Any())
			////{
			////	this.Context.RemoveRange(items);
			////}

			////await this.Context.SaveChangesAsync<MailQueue>();
		}

		#endregion

		public async Task<UserSubscriptions> ListUserSubscriptions(Guid userId)
		{
			UserSubscriptions results = new();
			List<UserSubscriptions.ForumGroupUserSubscription> groups = new();
			List<UserSubscriptions.ForumUserSubscription> forums = new();
			List<UserSubscriptions.PostUserSubscription> posts = new();

			foreach (ForumGroupSubscription subscription in await this.Context.ForumGroupSubscriptions
				.Where(item => item.User.Id == userId)
				.AsNoTracking()
				.ToListAsync())
			{
				Group group = await GetGroup(subscription.ForumGroupId);
				groups.Add(new() { Subscription = subscription, Group = group });
			}

			results.GroupSubscriptions = groups.OrderBy(groupSubscription => groupSubscription.Group.SortOrder).ToList();

			foreach (ForumSubscription subscription in await this.Context.ForumSubscriptions
				.Where(item => item.User.Id == userId)
				.AsNoTracking()
				.ToListAsync())
			{
				Forum forum = await GetForum(subscription.ForumId);
				forums.Add(new() { Subscription = subscription, Forum = forum });
			}

			results.ForumSubscriptions = forums.OrderBy(forumSubscription => forumSubscription.Forum.SortOrder).ToList();

			foreach (PostSubscription subscription in await this.Context.PostSubscriptions
				.Where(item => item.User.Id == userId)
				.AsNoTracking()
				.ToListAsync())
			{
				Post post = await GetForumPost(subscription.ForumPostId);
				posts.Add(new() { Subscription = subscription, Post = post });
			}

			results.PostSubscriptions = posts.OrderByDescending(postSubscription => postSubscription.Post.DateAdded).ToList();

			return results;
		}
	}
}

