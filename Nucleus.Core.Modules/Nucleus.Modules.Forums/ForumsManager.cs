using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions.Authorization;
using Nucleus.Modules.Forums.DataProviders;
using Nucleus.Modules.Forums.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Nucleus.Data.Common;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Forum"/>s.
	/// </summary>
	public class ForumsManager
	{
		public static class PermissionScopeNamespaces
		{
			public const string Forum = Nucleus.Modules.Forums.Models.Forum.URN + "/permissiontype";
		}
		public static class PermissionScopes
		{
			public static string FORUM_VIEW = $"{PermissionScopeNamespaces.Forum}/{PermissionType.PermissionScopeTypes.VIEW}";
			public static string FORUM_EDIT_POST = $"{PermissionScopeNamespaces.Forum}/{PermissionType.PermissionScopeTypes.EDIT}";
			public const string FORUM_CREATE_POST = PermissionScopeNamespaces.Forum + "/createpost";
			public const string FORUM_REPLY_POST = PermissionScopeNamespaces.Forum + "/reply";

			public const string FORUM_DELETE_POST = PermissionScopeNamespaces.Forum + "/delete";
			public const string FORUM_LOCK_POST = PermissionScopeNamespaces.Forum + "/lock";
			public const string FORUM_ATTACH_POST = PermissionScopeNamespaces.Forum + "/attach";
			public const string FORUM_SUBSCRIBE = PermissionScopeNamespaces.Forum + "/subscribe";

			public const string FORUM_PIN_POST = PermissionScopeNamespaces.Forum + "/pin";
			public const string FORUM_MODERATE = PermissionScopeNamespaces.Forum + "/moderate";
		}

		private IDataProviderFactory DataProviderFactory { get; }
		private ICacheManager CacheManager { get; }
		private IFileSystemManager FileSystemManager { get; }

		private IUserManager UserManager { get; }
		private IListManager ListManager { get; }

		private IPermissionsManager PermissionsManager { get; }

		public ForumsManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager, IPermissionsManager permissionsManager, IListManager listManager, IFileSystemManager fileSystemManager, IUserManager userManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
			this.FileSystemManager = fileSystemManager;
			this.UserManager = userManager;
			this.PermissionsManager = permissionsManager;
			this.ListManager = listManager;

		}

		/// <summary>
		/// Create a new <see cref="Forum"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="Forum"/> is not saved to the database until you call <see cref="SaveForum(Forum)"/>.
		/// </remarks>
		public Task<Forum> CreateForum(Group group)
		{
			Forum result = new() { Name = "New Forum" };
			result.Settings = new();
			return Task.FromResult(result);
		}


		/// <summary>
		/// Retrieve an existing <see cref="Forum"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<Forum> Get(Guid id)
		{
			return await this.CacheManager.ForumsCache().GetAsync(id, async id =>
			{
				using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
				{
					Forum forum = await provider.GetForum(id);
					if (forum != null)
					{
						if (forum.Settings == null)
						{
							forum.Settings = new();
						}
						await CheckPermissions(forum);
					}
					return forum;
				}

			});
		}

		private async Task CheckPermissions(Forum forum)
		{
			List<PermissionType> permissionTypes = await this.PermissionsManager.ListPermissionTypes(Forum.URN);
			Dictionary<Role, IList<Permission>> results = new();

			// ensure that for each role with any permissions defined, there is a full set of permission types for the role
			foreach (Role role in forum.Permissions.Select((permission) => permission.Role).ToList())
			{
				foreach (PermissionType permissionType in permissionTypes)
				{
					if (forum.Permissions.Where((permission) => permission?.Role.Id == role.Id && permission?.PermissionType.Id == permissionType.Id).ToList().Count == 0)
					{
						Permission permission = new();
						permission.AllowAccess = false;
						permission.PermissionType = permissionType;
						permission.Role = role;
						forum.Permissions.Add(permission);
					}
				}
			}
		}

		/// <summary>
		/// Delete the specifed <see cref="Forum"/> from the database.
		/// </summary>
		/// <param name="Forums"></param>
		public async Task Delete(Forum forum)
		{
			await this.PermissionsManager.DeletePermissions(forum.Permissions);

			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.DeleteForum(forum);
				this.CacheManager.ForumsCache().Remove(forum.Id);
				// forums belong to groups, remove group from cache too
				this.CacheManager.GroupsCache().Remove(forum.Group.Id);
			}
		}

		/// <summary>
		/// List all <see cref="Forum"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<IList<Forum>> List(Group group)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				return await provider.ListForums(group);
			}
		}

		/// <summary>
		/// List all moderator users for the specified forum.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<IList<User>> ListForumModerators(Forum forum)
		{
			List<User> users = new();

			IEnumerable<Permission> permissions;
			
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				if (forum.UseGroupSettings)
				{
					permissions = forum.Group.Permissions.Where(permission => permission.PermissionType.Scope == ForumsManager.PermissionScopes.FORUM_MODERATE && permission.AllowAccess);
				}
				else
				{
					permissions = forum.Permissions.Where(permission => permission.PermissionType.Scope == ForumsManager.PermissionScopes.FORUM_MODERATE && permission.AllowAccess);
				}
			}

			// List roles with permission(s)
			foreach (Role role in permissions.Select(permission => permission.Role).Distinct())
			{
				foreach (User user in await this.UserManager.ListUsersInRole(role))
				{
					// List distinct users in role(s)
					if (!users.Where(existing => existing.Id == user.Id).Any())
					{
						users.Add(user);
					}
				}
			}
			
			return users;
		}



		/// <summary>
		/// Create or update a <see cref="Forum"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="Forums"></param>
		public async Task Save(Group group, Forum forum)
		{			
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.SaveForum(group, forum);

				if (forum.Permissions != null)
				{
					// save permissions
					List<Permission> originalPermissions = await this.PermissionsManager.ListPermissions(forum.Id, ForumsManager.PermissionScopeNamespaces.Forum);
					await this.PermissionsManager.SavePermissions(forum.Id, forum.Permissions, originalPermissions);
				}

				this.CacheManager.ForumsCache().Remove(forum.Id);
				// forums belong to groups, remove group from cache too
				this.CacheManager.GroupsCache().Remove(group.Id);
			}
		}

		/// <summary>
		/// Add default permissions to the specifed <see cref="Forum"/> for the specified <see cref="Role"/>.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="role"></param>
		/// <remarks>
		/// The new permissions are not saved unless you call <see cref="Save(Site, Page)"/>.
		/// </remarks>
		public async Task CreatePermissions(Forum forum, Role role)
		{
			{
				List<PermissionType> permissionTypes = await this.PermissionsManager.ListPermissionTypes(PermissionScopeNamespaces.Forum);
				List<Permission> permissions = new();

				foreach (PermissionType permissionType in permissionTypes)
				{
					Permission permission = new();
					permission.AllowAccess = false;
					permission.PermissionType = permissionType;
					permission.Role = role;

					permissions.Add(permission);
				}

				forum.Permissions.AddRange(permissions);
			}

		}
		
		public Boolean CheckPermission(Site site, ClaimsPrincipal user, Forum forum, string permissionScope)
		{
			if (user.IsSystemAdministrator() || user.IsSiteAdmin(site))
			{
				return true;
			}
			else
			{
				foreach (Permission permission in forum.UseGroupSettings ? forum.Group.Permissions : forum.Permissions)
				{
					if (permission.PermissionType.Scope == permissionScope)
					{
						if (permission.IsValid(site, user))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Retrieve an existing <see cref="Forum"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<Post> GetForumPost(Guid forumPostId)
		{
			Post post;
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				post = await provider.GetForumPost(forumPostId);
			}

			return post;
		}

		/// <summary>
		/// Retrieve an existing <see cref="Forum"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<Reply> GetForumPostReply(Guid replyId)
		{
			Reply reply;
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				reply = await provider .GetForumPostReply(replyId);
			}

			return reply;
		}


		public async Task<IList<Post>> ListPosts(Forum forum, ClaimsPrincipal user, Models.FlagStates approved)
		{
			IList<Post> posts;

			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				posts = await provider.ListForumPosts(forum, approved);

				if (user != null)
				{
					foreach (Post post in posts)
					{
						post.Tracking = await GetPostTracking(post, user);
					}
				}
			}

			return posts;
		}

		/// <summary>
		/// Save a <see cref="Post"/>.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		public async Task SavePost(Site site, Forum forum, Post post)
		{
			List<Attachment> originalAttachments;

			if (post.Id == Guid.Empty)
			{
				if (!forum.EffectiveSettings().IsModerated)
				{
					post.IsApproved = true;
				}

				if (forum.EffectiveSettings().StatusList != null)
				{
					// Status list won't be fully populated on postback so we have to read it 
					forum.EffectiveSettings().StatusList = await this .ListManager.Get(forum.EffectiveSettings().StatusList.Id);
					post.Status = forum.EffectiveSettings().StatusList.Items.Where(item => item.Value.Equals("default", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
				}
			}

			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				// List attachments before save, so we can compare to the post, to delete files for removed attachments
				originalAttachments = await provider .ListPostAttachments(post.Id);
			
				await provider.SaveForumPost(forum, post);

				foreach (Attachment original in originalAttachments)
				{
					if (!post.Attachments.Where(attachment => attachment.Id == original.Id).Any())
					{
						// delete the file
						await this.FileSystemManager.DeleteFile(site, await this.FileSystemManager.GetFile(site, original.File.Id));
					}
				}
			}
		}

		/// <summary>
		/// Approve a <see cref="Post"/>.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		public async Task ApproveForumPost(Post post, Boolean value)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.SetForumPostApproved(post, value);
			}
		}

		/// <summary>
		/// Reject a <see cref="Post"/>.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		public async Task RejectForumPost(Forum forum, Post post)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.SetForumPostRejected(post, true);

				if (forum.EffectiveSettings().StatusList != null)
				{
					// Status list won't be fully populated on postback so we have to read it 
					forum.EffectiveSettings().StatusList = await this.ListManager.Get(forum.EffectiveSettings().StatusList.Id);
					await provider.SetForumPostStatus(post, forum.EffectiveSettings().StatusList.Items.Where(item => item.Value.Equals("rejected", StringComparison.OrdinalIgnoreCase)).FirstOrDefault());					
				}
			}
		}

		/// <summary>
		/// Approve a <see cref="Post"/>.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		public async Task SetForumPostStatus(Post post, Nucleus.Abstractions.Models.ListItem value)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.SetForumPostStatus(post, value);
			}
		}

		/// <summary>
		/// Pin a <see cref="Post"/>.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		public async Task PinForumPost(Post post, Boolean value)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.SetForumPostPinned(post, value);
			}
		}

		/// <summary>
		/// Lock a <see cref="Post"/>.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		public async Task LockForumPost(Post post, Boolean value)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.SetForumPostLocked(post, value);
			}
		}

		/// <summary>
		/// Delete a <see cref="Post"/>.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		public async Task DeleteForumPost(Forum forum, Post post)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.DeletePostAttachments(post);
			}

			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.DeleteForumPost(post);
			}

		}

		/// <summary>
		/// Save a <see cref="Reply"/>.
		/// </summary>
		/// <param name="post"></param>
		/// <param name="reply"></param>
		/// <returns></returns>
		public async Task SavePostReply(Site site, Post post, Reply reply)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				if (reply.Id == Guid.Empty)
				{
					Models.Forum forum = await provider.GetForum(post.ForumId);
					reply.IsApproved = !forum.EffectiveSettings().IsModerated;
				}

				// List attachments before save, so we can compare to the post, to delete files for removed attachments
				List<Attachment> originalAttachments = await provider.ListReplyAttachments(post.Id, reply.Id);

				await provider.SaveForumPostReply(post, reply);

				foreach (Attachment original in originalAttachments)
				{
					if (!reply.Attachments.Where(attachment => attachment.Id == original.Id).Any())
					{
						// delete the file
						await this.FileSystemManager.DeleteFile(site, await this.FileSystemManager.GetFile(site, original.File.Id));
					}
				}
			}
		}

		/// <summary>
		/// Delete a <see cref="Reply"/>.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		public async Task DeletePostReply(Forum forum, Reply reply)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.DeleteReplyAttachments(reply);
			}

			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.DeleteForumPostReply(reply);
			}
		}

		public async Task<IList<Reply>> ListPostReplies(Site site, Post post, FlagStates approved)
		{
			IList<Reply> results;

			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				results = await provider .ListForumPostReplies(post, approved);
			}

			return results;
		}

		/// <summary>
		/// Subscribe the specifed user to the specified <see cref="Forum"/>.
		/// </summary>
		/// <param name="Forum"></param>
		public async Task Subscribe(Forum forum, ClaimsPrincipal user)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.SubscribeForum(forum.Id, user.GetUserId());
			}
		}

		/// <summary>
		/// Un-subscribe the specifed user from the specified <see cref="Forum"/>.
		/// </summary>
		/// <param name="Forum"></param>
		public async Task UnSubscribe(Forum forum, ClaimsPrincipal user)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.UnSubscribeForum(forum.Id, user.GetUserId());
			}
		}

		/// <summary>
		/// Subscribe the specifed user to the specified <see cref="Forum"/>.
		/// </summary>
		/// <param name="Forum"></param>
		public async Task Subscribe(Post post, ClaimsPrincipal user)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.SubscribeForumPost(post.Id, user.GetUserId());
			}
		}

		/// <summary>
		/// Un-subscribe the specifed user from the specified <see cref="Forum"/>.
		/// </summary>
		/// <param name="Post"></param>
		public async Task UnSubscribe(Post post, ClaimsPrincipal user)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.UnSubscribeForumPost(post.Id, user.GetUserId());
			}
		}

		/// <summary>
		/// Retrieve an existing <see cref="ForumSubscription"/> from the database, or return null if there is no matching record.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public async Task<ForumSubscription> GetSubscription(Forum forum, ClaimsPrincipal user)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				return await provider.GetForumSubscription(forum.Id, user.GetUserId()); 
			}
		}

		/// <summary>
		/// Retrieve a list of users who are subscribed to the specified forum.
		/// </summary>
		/// <param name="forum"></param>
		/// <returns></returns>
		public async Task<List<User>> ListForumSubscribers(Guid forumId)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				return await provider.ListForumSubscribers(forumId);
			}
		}

		/// <summary>
		/// Retrieve a list of users who are subscribed to the specified forum post.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public async Task<List<User>> ListPostSubscribers(Guid postId)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				return await provider.ListPostSubscribers(postId);
			}
		}


		/// <summary>
		/// Retrieve an existing <see cref="ForumSubscription"/> from the database, or return null if there is no matching record.
		/// </summary>
		/// <param name="post"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public async Task<PostSubscription> GetSubscription(Post post, ClaimsPrincipal user)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				return await provider.GetPostSubscription(post.Id, user.GetUserId());
			}

		}

		public async Task<PostTracking> GetPostTracking(Post post, ClaimsPrincipal user)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				return await provider.GetPostTracking(post.Id, user.GetUserId());
			}
		}
		
		public async Task SavePostTracking(Post post, ClaimsPrincipal user)
		{
			if (user.Identity.IsAuthenticated)
			{
				using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
				{
					await provider.SavePostTracking(post.Id, user.GetUserId());
				}
			}
		}

		public async Task DeletePostTracking(Post post, ClaimsPrincipal user)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.DeletePostTracking(post.Id, user.GetUserId());
			}
		}

		public async Task SaveMailQueue(MailQueue mailQueue)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				if (!await provider.IsQueued(mailQueue))
				{
					await provider.SaveMailQueue(mailQueue);
				}
			}
		}

		public async Task<IList<MailQueue>> ListMailQueue()
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				return await provider.ListMailQueue();				
			}
		}

		public async Task SetMailQueueStatus(MailQueue mailQueue)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.SetMailQueueStatus(mailQueue);
			}
		}

		public async Task TruncateMailQueue(TimeSpan sentBefore)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				await provider.TruncateMailQueue(sentBefore);
			}
		}
	}

}
