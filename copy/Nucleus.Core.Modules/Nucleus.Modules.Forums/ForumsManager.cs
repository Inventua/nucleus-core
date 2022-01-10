using Nucleus.Abstractions.Models;
using Nucleus.Core;
using Nucleus.Core.Authorization;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Modules.Forums.DataProviders;
using Nucleus.Modules.Forums.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
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
			public const string FORUM_VIEW = PermissionScopeNamespaces.Forum + "/view";
			public const string FORUM_EDIT_POST = PermissionScopeNamespaces.Forum + "/edit";
			public const string FORUM_CREATE_POST = PermissionScopeNamespaces.Forum + "/createpost";
			public const string FORUM_REPLY_POST = PermissionScopeNamespaces.Forum + "/reply";

			public const string FORUM_DELETE_POST = PermissionScopeNamespaces.Forum + "/delete";
			public const string FORUM_LOCK_POST = PermissionScopeNamespaces.Forum + "/lock";
			public const string FORUM_ATTACH_POST = PermissionScopeNamespaces.Forum + "/attach";
			public const string FORUM_SUBSCRIBE = PermissionScopeNamespaces.Forum + "/subscribe";

			public const string FORUM_PIN_POST = PermissionScopeNamespaces.Forum + "/pin";
			public const string FORUM_MODERATE = PermissionScopeNamespaces.Forum + "/moderate";
		}

		private DataProviderFactory DataProviderFactory { get; }
		private CacheManager CacheManager { get; }
		private FileSystemManager FileSystemManager { get; }


		public ForumsManager(DataProviderFactory dataProviderFactory, CacheManager cacheManager, FileSystemManager fileSystemManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
			this.FileSystemManager = fileSystemManager;
			// todo: get cacheoption settings from config
			this.CacheManager.Add<Guid, Forum>(new Nucleus.Abstractions.Models.Configuration.CacheOption());
		}

		/// <summary>
		/// Create a new <see cref="Forum"/> with default values.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// The new <see cref="Forum"/> is not saved to the database until you call <see cref="SaveForum(Forum)"/>.
		/// </remarks>
		public static Forum CreateForum(Group group)
		{
			Forum result = new() { Name = "New Forum" };

			return result;
		}


		/// <summary>
		/// Retrieve an existing <see cref="Forum"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Forum Get(Guid id)
		{
			return this.CacheManager.ForumsCache().Get(id, id =>
			{
				using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
				{
					Forum forum = provider.GetForum(id);
					if (forum != null)
					{
						if (forum.UseGroupSettings)
						{
							Group group = provider.GetGroup(provider.GetForumGroupId(forum));

							forum.Permissions = group.Permissions;
							forum.Settings = group.Settings;
							forum.AttachmentsFolder = group.AttachmentsFolder;
						}
						else
						{
							forum.Permissions = this.ListPermissions(forum);
						}

						using (IUserDataProvider userProvider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
						{
							if (forum?.Statistics?.LastPost != null)
							{
								forum.Statistics.LastPost.PostedBy = userProvider.GetUser(forum.Statistics.LastPost.AddedBy)?.UserName;
							}
						}						
					}

					return forum;
				}
			});
		}

		/// <summary>
		/// Delete the specifed <see cref="Forum"/> from the database.
		/// </summary>
		/// <param name="Forums"></param>
		public void Delete(Forum forum)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				Guid groupId = provider.GetForumGroupId(forum);

				provider.DeleteForum(forum);
				this.CacheManager.ForumsCache().Remove(forum.Id);
				// forums belong to groups, remove group from cache too
				this.CacheManager.GroupsCache().Remove(groupId);
			}
		}

		/// <summary>
		/// List all <see cref="Forum"/>s within the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IList<Forum> List(Group group)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				return provider.ListForums(group);
			}
		}

		/// <summary>
		/// Create or update a <see cref="Forum"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="Forums"></param>
		public void Save(Group group, Forum forum)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				provider.SaveForum(group, forum);

				if (forum.Permissions != null)
				{
					// save permissions
					using (IPermissionsDataProvider permissionsProvider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
					{
						List<Permission> originalPermissions = permissionsProvider.ListPermissions(forum.Id, ForumsManager.PermissionScopeNamespaces.Forum);
						permissionsProvider.SavePermissions(forum.Id, forum.Permissions, originalPermissions);
					}
				}

				this.CacheManager.ForumsCache().Remove(forum.Id);
				// forums belong to groups, remove group from cache too
				this.CacheManager.GroupsCache().Remove(provider.GetForumGroupId(forum));
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
		public void CreatePermissions(Forum forum, Role role)
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				{
					List<PermissionType> permissionTypes = provider.ListPermissionTypes(PermissionScopeNamespaces.Forum);
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
		}

		public Boolean CheckPermission(Site site, ClaimsPrincipal user, Forum forum, string permissionScope)
		{
			if (user.IsSystemAdministrator() || user.IsSiteAdmin(site))
			{
				return true;
			}
			else
			{
				foreach (Permission permission in forum.Permissions)
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

		public List<Permission> ListPermissions(Forum forum)
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				return provider.ListPermissions(forum.Id, PermissionScopeNamespaces.Forum);
			}
		}

		public List<Permission> ListGroupPermissions(Forum forum)
		{
			Guid groupId;

			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				groupId = provider.GetForumGroupId(forum);
			}

			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				return provider.ListPermissions(forum.Id, PermissionScopeNamespaces.Forum);
			}
		}

		/// <summary>
		/// Retrieve an existing <see cref="Forum"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Post GetForumPost(Guid forumPostId)
		{
			Post post;
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				post = provider.GetForumPost(forumPostId);
			}

			if (post != null)
			{
				using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
				{
					post.PostedBy = provider.GetUser(post.AddedBy)?.UserName;
				}

			}

			return post;
		}

		/// <summary>
		/// Retrieve an existing <see cref="Forum"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Reply GetForumPostReply(Guid replyId)
		{
			Reply reply;
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				reply = provider.GetForumPostReply(replyId);
			}

			if (reply != null)
			{
				using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
				{
					reply.PostedBy = provider.GetUser(reply.AddedBy)?.UserName;
				}
			}

			return reply;
		}



		public IList<Post> ListPosts(Forum forum, Models.FlagStates approved)
		{
			IList<Post> posts;

			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				posts = provider.ListForumPosts(forum, approved);
			}

			using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
			{
				foreach (Post post in posts)
				{
					post.PostedBy = provider.GetUser(post.AddedBy)?.UserName;
					if (post?.Statistics?.LastReply != null)
					{
						post.Statistics.LastReply.PostedBy = provider.GetUser(post.Statistics.LastReply.AddedBy)?.UserName;
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
		public void SavePost(Site site, Forum forum, Post post)
		{
			if (post.Id == Guid.Empty)
			{
				post.IsApproved = !forum.Settings.IsModerated;
				if (forum.Settings.StatusList != null)
				{
					post.Status = forum.Settings.StatusList.Items.Where(item => item.Value.Equals("default", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
				}
			}

			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				// List attachments before save, so we can compare to the post, to delete files for removed attachments
				List<Attachment> originalAttachments = provider.ListPostAttachments(post.Id);

				provider.SaveForumPost(forum, post);

				foreach (Attachment original in originalAttachments)
				{
					if (!post.Attachments.Where(attachment=>attachment.Id == original.Id).Any())
					{
						// delete the file
						this.FileSystemManager.DeleteFile(site, this.FileSystemManager.GetFile(site, original.File.Id));
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
		public void ApproveForumPost(Post post, Boolean value)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				provider.SetForumPostApproved(post, value);
			}
		}

		/// <summary>
		/// Reject a <see cref="Post"/>.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		public void RejectForumPost(Forum forum, Post post)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				provider.SetForumPostApproved(post, false);
				if (forum.Settings.StatusList != null)
				{
					post.Status = forum.Settings.StatusList.Items.Where(item => item.Value.Equals("rejected", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
				}
			}
		}

		/// <summary>
		/// Approve a <see cref="Post"/>.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		public void SetForumPostStatus(Post post, Nucleus.Abstractions.Models.ListItem value)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				provider.SetForumPostStatus(post, value);
			}
		}

		/// <summary>
		/// Pin a <see cref="Post"/>.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		public void PinForumPost(Post post, Boolean value)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				provider.SetForumPostPinned(post, value);
			}
		}

		/// <summary>
		/// Lock a <see cref="Post"/>.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		public void LockForumPost(Post post, Boolean value)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				provider.SetForumPostLocked(post, value);
			}
		}

		/// <summary>
		/// Delete a <see cref="Post"/>.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		public void DeleteForumPost(Forum forum, Post post)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				provider.DeleteForumPost(post);
			}

			// todo: delete attachment files
		}

		/// <summary>
		/// Save a <see cref="Reply"/>.
		/// </summary>
		/// <param name="post"></param>
		/// <param name="reply"></param>
		/// <returns></returns>
		public void SavePostReply(Site site, Post post, Reply reply)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				if (reply.Id == Guid.Empty)
				{
					Models.Forum forum = provider.GetForum(post.ForumId);
					reply.IsApproved = !forum.Settings.IsModerated;
				}

				// List attachments before save, so we can compare to the post, to delete files for removed attachments
				List<Attachment> originalAttachments = provider.ListReplyAttachments(post.Id, reply.Id);

				provider.SaveForumPostReply(post, reply);

				foreach (Attachment original in originalAttachments)
				{
					if (!reply.Attachments.Where(attachment => attachment.Id == original.Id).Any())
					{
						// delete the file
						this.FileSystemManager.DeleteFile(site, this.FileSystemManager.GetFile(site, original.File.Id));
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
		public void DeletePostReply(Forum forum, Reply reply)
		{
			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				provider.DeleteForumPostReply(reply);
			}
			// todo: delete attachment files
		}

		public IList<Reply> ListPostReplies(Post post, FlagStates approved)
		{
			IList<Reply> results;

			using (IForumsDataProvider provider = this.DataProviderFactory.CreateProvider<IForumsDataProvider>())
			{
				results = provider.ListForumPostReplies(post, approved);
			}

			if (results != null)
			{
				using (IUserDataProvider provider = this.DataProviderFactory.CreateProvider<IUserDataProvider>())
				{
					foreach (Reply reply in results)
					{
						reply.PostedBy = provider.GetUser(reply.AddedBy)?.UserName;
					}
				}
			}

			return results;
		}


	}

}
