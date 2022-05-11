using System;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.TaskScheduler;
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
using Nucleus.Abstractions.Managers;
using System.Threading.Tasks;

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
				.FirstOrDefaultAsync();

			if (result != null)
			{
				result.Settings = await GetSettings(result.Id);				
			}

			return result;
		}

		public async Task<IList<Group>> ListGroups(PageModule pageModule)
		{
			IList<Group> results = await this.Context.Groups
				.Where(group => EF.Property<Guid>(group, "ModuleId") == pageModule.Id)
				.OrderBy(group => group.SortOrder)
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

			Boolean isNew = !this.Context.Groups.Where(existing => existing.Id == group.Id).Any();

			this.Context.Attach(group);
			this.Context.Entry(group).Property("ModuleId").CurrentValue = module.Id;

			if (isNew)
			{
				group.SortOrder = await GetTopForumGroupSortOrder(module.Id) + 10;
				this.Context.Entry(group).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Group, Create>(group); });
			}
			else
			{
				this.Context.Entry(group).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Group, Update>(group); });
			}

			await this.Context.SaveChangesAsync();
			
			await SaveSettings(group.Id, group.Settings);

			group.Permissions = permissions;

			raiseEvent.Invoke();
		}

		public async Task DeleteGroup(Group group)
		{
			if (group.Forums != null)
			{
				foreach (Forum forum in group.Forums)
				{
					await DeleteForum(forum);
					this.Context.ChangeTracker.Clear();
				}
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
				.CountAsync();

			result.ReplyCount = await this.Context.Posts
				.Where(post => post.ForumId == forumId)
				.SelectMany(post => post.Replies)
				.CountAsync();

			result.LastPost = await GetLastPost(forumId);

			return result;
		}

		private async Task<Post> GetLastPost(Guid forumId)
		{
			return await this.Context.Posts
				.Where(post => post.ForumId == forumId)
				.Include(post => post.PostedBy)
				.OrderByDescending(post => post.DateAdded)
				.FirstOrDefaultAsync();
		}

		public async Task<IList<Forum>> ListForums(Group group)
		{
			IList<Forum> results = await this.Context.Forums
				.Where(forum => forum.Group.Id == group.Id)
				.Include(forum => forum.Group)
					.ThenInclude(group => group.Permissions)
						.ThenInclude(permission => permission.Role)
				.Include(forum => forum.Group)
					.ThenInclude(group => group.Permissions)
						.ThenInclude(permission => permission.PermissionType)
				.Include(forum => forum.Group)
				.OrderBy(forum => forum.SortOrder)
				.ToListAsync();

			foreach (Forum result in results)
			{
				result.Settings = await GetSettings(result.Id);
				result.Statistics = await GetForumStatistics(result.Id);

				result.Group.Settings = await GetSettings(result.Group.Id);
			}

			return results;
		}


		public async Task SaveForum(Group group, Forum forum)
		{
			Action raiseEvent;
			
			// We have to store/NULL/restore the permissions collection, otherwise EF tries to update it, and we want to handle saving permissions
			// in .SavePermissions
			List<Permission> permissions = forum.Permissions;
			forum.Permissions = null;

			Boolean isNew = !this.Context.Forums.Where(existing => existing.Id == forum.Id).Any();
			
			this.Context.Attach(forum);
			this.Context.Entry(forum).Property("ForumGroupId").CurrentValue = group.Id;

			if (isNew)
			{
				forum.SortOrder = await GetTopForumSortOrder(group.Id) + 10;
				this.Context.Entry(forum).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Forum, Create>(forum); });
			}
			else
			{
				this.Context.Entry(forum).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Forum, Update>(forum); });
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
			Settings settings = this.Context.Settings.Where(setting => EF.Property<Guid>(setting, "RelatedId") == forum.Id).FirstOrDefault();
			
			if (settings != null)
			{
				this.Context.Entry(settings).State = EntityState.Deleted;
			}
			
			this.Context.Remove(forum);
			await this.Context.SaveChangesAsync();
		}

		private async Task<int> GetTopForumSortOrder(Guid groupId)
		{
			Forum forum = await this.Context.Forums
				.Where(forum => forum.Group.Id == groupId)
				.OrderByDescending(forum => forum.SortOrder)
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

				result.Statistics = await GetPostStatistics(id);
			}

			return result;
		}

		public async Task<IList<Post>> ListForumPosts(Forum forum, FlagStates approved)
		{
			IList<Post> results = await this.Context.Posts
				.Where(post => post.ForumId == forum.Id && (approved == FlagStates.IsAny || (post.IsApproved == (approved == FlagStates.IsTrue))))
				.Include(post => post.Status)
				.Include(post => post.Attachments)
					.ThenInclude(attachment => attachment.File)
				.Include(post => post.PostedBy)
				.OrderByDescending(post => post.IsPinned)
				.ThenByDescending(post => post.DateAdded)
				.AsSplitQuery()
				.ToListAsync();

			foreach (Post result in results)
			{
				result.Statistics = await GetPostStatistics(result.Id);
			}

			return results;
		}

		public async Task DeleteForumPost(Post post)
		{
			this.Context.Posts.Remove(post);
			await this.Context.SaveChangesAsync<Post>();
		}

		public async Task SaveForumPost(Forum forum, Post post)
		{
			Action raiseEvent;
			
			Boolean isNew = !this.Context.Posts.Where(existing => existing.Id == post.Id).Any();
			
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
				this.Context.Entry(post).Property(post => post.IsLocked).IsModified = false;
				this.Context.Entry(post).Property(post => post.IsPinned).IsModified = false;
				this.Context.Entry(post).Reference(post => post.Status).IsModified = false;

				// Forum id can't be changed for an existing post (if we implement a "move" function in the future, it will use a special function)
				this.Context.Entry(post).Property(post => post.ForumId).IsModified = false;

				raiseEvent = new(() => { this.EventManager.RaiseEvent<Post, Update>(post); });
			}

			await this .Context.SaveChangesAsync<Post>();

			List<Attachment> currentAttachments = this.Context.Attachments
				.Where(attachment => EF.Property<Guid>(attachment, "ForumPostId") == post.Id && EF.Property<Guid?>(attachment, "ForumReplyId") == null)
				.AsNoTracking()
				.ToList();

			await SaveAttachments(post.Id, null, post.Attachments, currentAttachments);

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
					if (!attachments.Where(attachment => attachment.Id == original.Id).Any())
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
						if (attachment.Id == originalAttachment.Id)
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

		private async Task<PostStatistics> GetPostStatistics(Guid postId)
		{
			PostStatistics result = new();

			result.ReplyCount = await this.Context.Replies
				.Where(reply => reply.Post.Id == postId)
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
				.FirstOrDefaultAsync();
		}

		public async Task<IList<Reply>> ListForumPostReplies(Post post, FlagStates approved)
		{
			return await this.Context.Replies
				.Where(reply => reply.Post.Id == post.Id && (approved == FlagStates.IsAny || (post.IsApproved == (approved == FlagStates.IsTrue))))
				.Include(reply => reply.Attachments)
					.ThenInclude(attachment => attachment.File)
				.Include(reply => reply.Post)
				.Include(reply => reply.ReplyTo)
				.Include(reply => reply.PostedBy)
				.AsSplitQuery()
				.OrderBy(reply => reply.DateAdded)
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

			this.Context.Attach(reply);
			this.Context.Entry(reply).Property("ForumPostId").CurrentValue = post.Id;

			if (isNew)
			{
				this.Context.Entry(reply).State = EntityState.Added;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Reply, Create>(reply); });
			}
			else
			{
				this.Context.Entry(reply).State = EntityState.Modified;
				raiseEvent = new(() => { this.EventManager.RaiseEvent<Reply, Update>(reply); });
			}

			await this.Context.SaveChangesAsync<Reply>();

			List<Attachment> currentAttachments = await this.Context.Attachments
				.Where(attachment => EF.Property<Guid>(attachment, "ForumPostId") == post.Id && EF.Property<Guid>(attachment, "ForumReplyId") == reply.Id)
				.AsNoTracking()
				.ToListAsync();

			await SaveAttachments(post.Id, reply.Id, reply.Attachments, currentAttachments);

			raiseEvent.Invoke();
		}

		private async Task<Reply> GetLastReply(Guid postId)
		{
			return await this.Context.Replies
				.Where(reply => reply.Post.Id == postId)
				.Include(reply => reply.Post)
				.Include(reply => reply.ReplyTo)
				.Include(reply => reply.PostedBy)
				.AsSplitQuery()
				.OrderByDescending(reply => reply.DateAdded)
				.FirstOrDefaultAsync();

		}


		#endregion

		#region "    Subscriptions    "

		public async Task SubscribeForum(Guid forumId, Guid userId)
		{
			Boolean alreadyExists = await this.Context.ForumSubscriptions
				.Where(subscription => subscription.ForumId == forumId && subscription.UserId == userId)
				.AnyAsync();

			if (!alreadyExists)
			{
				this.Context.Add(new ForumSubscription() { ForumId = forumId, UserId = userId });
				await this.Context.SaveChangesAsync<ForumSubscription>();
			}
		}

		public async Task UnSubscribeForum(Guid forumId, Guid userId)
		{
			ForumSubscription subscription = await this.Context.ForumSubscriptions
				.Where(subscription => subscription.ForumId == forumId && subscription.UserId == userId)
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
				
		public async Task<List<User>> ListForumSubscribers(Guid forumId)
		{
			List<Guid> subscriptionUsers = await this.Context.ForumSubscriptions
				.Where(subscription => subscription.ForumId == forumId)
				.Select(subscription => subscription.UserId)
				.ToListAsync();

			return await this.Context.Users
				.Where(user => subscriptionUsers.Contains(user.Id))
				.ToListAsync();

		}

		public async Task<ForumSubscription> GetForumSubscription(Guid forumId, Guid userId)
		{
			return await this.Context.ForumSubscriptions
				.Where(subscription => subscription.ForumId == forumId && subscription.UserId == userId)
				.FirstOrDefaultAsync();
		}

		public async Task SubscribeForumPost(Guid postId, Guid userId)
		{
			Boolean alreadyExists = await this.Context.PostSubscriptions
				.Where(subscription => subscription.ForumPostId == postId && subscription.UserId == userId)
				.AnyAsync();

			if (!alreadyExists)
			{
				this.Context.Add(new PostSubscription() { ForumPostId = postId, UserId = userId });
				await this.Context.SaveChangesAsync<PostSubscription>();
			}
		}

		public async Task UnSubscribeForumPost(Guid postId, Guid userId)
		{
			PostSubscription subscription = await this.Context.PostSubscriptions
				.Where(subscription => subscription.ForumPostId == postId && subscription.UserId == userId)
				.FirstOrDefaultAsync();

			if (subscription != null)
			{
				this.Context.Remove(subscription);
			}

			await this.Context.SaveChangesAsync<PostSubscription>();
		}

		public async Task<List<User>> ListPostSubscribers(Guid postId)
		{
			List<Guid> subscriptionUsers = await this.Context.PostSubscriptions
				.Where(subscription => subscription.ForumPostId == postId)
				.Select(subscription => subscription.UserId)
				.ToListAsync();

			return await this.Context.Users
				.Where(user => subscriptionUsers.Contains(user.Id))
				.ToListAsync();

		}


		public async Task<PostSubscription> GetPostSubscription(Guid postId, Guid userId)
		{
			return await this.Context.PostSubscriptions
				.Where(subscription => subscription.ForumPostId == postId && subscription.UserId == userId)
				.FirstOrDefaultAsync();
		}
		#endregion

		#region "    Post Tracking    "
		public async Task<PostTracking> GetPostTracking(Guid postId, Guid userId) 
		{
			return await this.Context.PostTracking
				.Where(tracking => tracking.ForumPostId == postId && tracking.UserId == userId)
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
				.Where(existing => existing.UserId == mailQueue.UserId && existing.MailTemplateId == mailQueue.MailTemplateId && existing.Post.Id == mailQueue.Post.Id && (mailQueue.Reply == null && existing.Reply == null) || (mailQueue.Reply != null && existing.Reply.Id == mailQueue.Reply.Id))
				.AnyAsync();
		}

		public async Task SaveMailQueue(MailQueue mailQueue)
		{
			MailQueue existing = await this.Context.MailQueue
				.Where(existing => existing.Id == mailQueue.Id)
				.FirstOrDefaultAsync();

			if (existing == null)
			{
				this.Context.Add(mailQueue);
			}
			else
			{
				this.Context.Entry(mailQueue).State = EntityState.Modified;
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

		public async Task<IList<MailQueue>> ListMailQueue()
		{
			return await this.Context.MailQueue
				.Where(item => item.Status == MailQueue.MailQueueStatus.Queued)
				.Include(item => item.Post)
					.ThenInclude(post => post.Status)
				.Include(item => item.Post)
					.ThenInclude(post => post.PostedBy)
				.Include(item => item.Reply)
					.ThenInclude(reply => reply.ReplyTo)
				.Include(item => item.Reply)
					.ThenInclude(reply => reply.PostedBy)
				.OrderBy(item => item.UserId)
					.ThenBy(item => item.ModuleId)
					.ThenBy(item => item.Post.ForumId)
					.ThenBy(item => item.MailTemplateId )
					.ThenBy(item => item.Post.Id)
					.ThenBy(item => item.Reply.Id)
					.ThenBy(item => item.DateAdded)
				.AsSplitQuery()
				.ToListAsync();
		}

		public async Task TruncateMailQueue(TimeSpan sentBefore)
		{
			DateTime beforeDate = DateTime.Now.Add(-sentBefore);

			IEnumerable<MailQueue> items = await this.Context.MailQueue
				.Where(item => item.Status == MailQueue.MailQueueStatus.Sent && item.DateChanged < beforeDate)
				.ToListAsync();

			if (items.Any())
			{
				this.Context.RemoveRange(items);
			}

			await this.Context.SaveChangesAsync<MailQueue>();
		}

		#endregion
	}
}

