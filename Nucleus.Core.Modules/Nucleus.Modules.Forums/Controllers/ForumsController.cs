using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Modules.Forums.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.Forums.Controllers
{
	[Extension("Forums")]
	public class ForumsController : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private GroupsManager GroupsManager { get; }
		private ForumsManager ForumsManager { get; }
		private IMailTemplateManager MailTemplateManager { get; }
		private IRoleManager RoleManager { get; }
		private IFileSystemManager FileSystemManager { get; }

		internal const string MANAGE_SUBSCRIPTIONS_PATH = "manage-subscriptions";

		public ForumsController(IWebHostEnvironment webHostEnvironment, Context Context, IPageModuleManager pageModuleManager, GroupsManager groupsManager, ForumsManager forumsManager, IMailTemplateManager mailTemplateManager, IRoleManager roleManager, IFileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.GroupsManager = groupsManager;
			this.ForumsManager = forumsManager;
			this.MailTemplateManager = mailTemplateManager;
			this.RoleManager = roleManager;
			this.FileSystemManager = fileSystemManager;
		}

		[HttpGet]
		public async Task<ActionResult> Index()
		{
			if (!this.Context.LocalPath.HasValue)
			{
				try
				{
					// display forum table of contents
					return View("ListForums", await BuildListForumsViewModel());
				}
				catch (UnauthorizedAccessException)
				{
					return Forbid();
				}
			}
			else
			{
				if (this.Context.LocalPath.Segments.FirstOrDefault() == MANAGE_SUBSCRIPTIONS_PATH)
				{
					return View("ManageSubscriptions", await BuildSubscriptionsViewModel());
				}

				Models.Forum forum = await this.GroupsManager.FindForum(this.Context.Module, this.Context.LocalPath.Segments[0]);
				if (forum != null)
				{
					if (this.Context.LocalPath.Segments.Length > 1)
					{
						return await ViewPost(Guid.Parse(this.Context.LocalPath.Segments[1]));
					}
					else
					{
						return await BuildViewForumView(forum.Id, new());
					}
				}
				else
				{
					return NotFound();
				}
			}
		}

		[HttpGet]
		public async Task<ActionResult> ManageSubscriptions()
		{
			if (User.Identity.IsAuthenticated)
			{
				if (User.IsApproved() && User.IsVerified())
				{
					return View("ManageSubscriptions", await BuildSubscriptionsViewModel());
				}
				else
				{
					return Forbid();
				}
			}
			else
			{
				return Unauthorized();
			}
		}

		[HttpPost]
		public async Task<ActionResult> ViewForum(ViewModels.ViewForum viewModel)
		{
			return await BuildViewForumView(viewModel.Forum.Id, viewModel.Posts);
		}

		private async Task<ActionResult> BuildViewForumView(Guid forumId, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			try
			{
				return View("ViewForum", await BuildViewForumViewModel(forumId, pagingSettings));
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}


		[HttpGet]
		public async Task<ActionResult> AddPost(Guid forumId)
		{
			ViewModels.ViewForumPost viewModel = await BuildPostViewModel(forumId, Guid.Empty, false);

			if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_CREATE_POST))
			{
				return View("EditPost", viewModel);
			}
			else
			{
				return Forbid();
			}
		}

		[HttpGet]
		public async Task<ActionResult> EditPost(Guid id)
		{
			ViewModels.ViewForumPost viewModel = await BuildPostViewModel(Guid.Empty, id, false);

			if (CanEditPost(viewModel.Forum, viewModel.Post))
			{
				return View("EditPost", viewModel);
			}
			else
			{
				return Forbid();
			}
		}

		[HttpPost]
		public async Task<ActionResult> AddPostAttachment(ViewModels.ViewForumPost viewModel, [FromForm] IFormFile mediaFile)
		{
			if (mediaFile != null)
			{
				viewModel.Forum = await this.ForumsManager.Get(viewModel.Forum.Id);

				viewModel.Forum.EffectiveSettings().AttachmentsFolder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Forum.EffectiveSettings().AttachmentsFolder.Id);
				using (System.IO.Stream fileStream = mediaFile.OpenReadStream())
				{
					Models.Attachment attachment = new()
					{
						File = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.Forum.EffectiveSettings().AttachmentsFolder.Provider, viewModel.Forum.EffectiveSettings().AttachmentsFolder.Path, mediaFile.FileName, fileStream, false)
					};

					viewModel.Post.Attachments.Add(attachment);
				}
			}
			else
			{
				return BadRequest();
			}

			return View("EditPost", await BuildPostViewModel(viewModel, true));
		}

		[HttpPost]
		public async Task<ActionResult> DeletePostAttachment(ViewModels.ViewForumPost viewModel, Guid id)
		{
			viewModel.Forum = await this.ForumsManager.Get(viewModel.Forum.Id);

			Models.Attachment attachment = viewModel.Post.Attachments.Where(attachment => attachment.Id == id).FirstOrDefault();
			if (attachment != null)
			{
				viewModel.Post.Attachments.Remove(attachment);
			}
			else
			{
				return BadRequest();
			}

			ModelState.RemovePrefix("Post.Attachments");

			viewModel = await BuildPostViewModel(viewModel, false);

			return View("EditPost", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> AddReplyAttachment(ViewModels.ReplyForumPost viewModel, [FromForm] IFormFile mediaFile)
		{
			if (mediaFile != null)
			{
				viewModel.Forum = await this.ForumsManager.Get(viewModel.Forum.Id);
				viewModel.Forum.EffectiveSettings().AttachmentsFolder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Forum.EffectiveSettings().AttachmentsFolder.Id);
				using (System.IO.Stream fileStream = mediaFile.OpenReadStream())
				{
					Models.Attachment attachment = new()
					{
						File = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.Forum.EffectiveSettings().AttachmentsFolder.Provider, viewModel.Forum.EffectiveSettings().AttachmentsFolder.Path, mediaFile.FileName, fileStream, false)
					};

					viewModel.Reply.Attachments.Add(attachment);
				}
			}
			else
			{
				return BadRequest();
			}

			return View("ReplyPost", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> DeleteReplyAttachment(ViewModels.ReplyForumPost viewModel, Guid id)
		{
			viewModel.Forum = await this.ForumsManager.Get(viewModel.Forum.Id);

			Models.Attachment attachment = viewModel.Reply.Attachments.Where(attachment => attachment.Id == id).FirstOrDefault();
			if (attachment != null)
			{
				viewModel.Reply.Attachments.Remove(attachment);
			}
			else
			{
				return BadRequest();
			}

			return View("ReplyPost", viewModel);
		}



		[HttpGet]
		public async Task<ActionResult> ViewPost(Guid id)
		{
			ViewModels.ViewForumPost viewModel = await BuildPostViewModel(Guid.Empty, id, true);

			if (viewModel.Forum != null && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_VIEW))
			{
				await this.ForumsManager.SavePostTracking(viewModel.Post, HttpContext.User);
				return View("ViewPost", viewModel);
			}
			else
			{
				return Forbid();
			}
		}

		[HttpPost]
		public async Task<ActionResult> SaveForumPost(ViewModels.ViewForumPost viewModel)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_CREATE_POST))
				{
					return Forbid();
				}
			}
			else if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_EDIT_POST))
			{
				return Forbid();
			}

			// make sure that pinned, locked status hasn't been set by users who don't have rights
			if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_PIN_POST))
			{
				if (viewModel.Post.IsPinned)
				{
					return Forbid();
				}
			}

			if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_LOCK_POST))
			{
				if (viewModel.Post.IsLocked)
				{
					return Forbid();
				}
			}

			await this.ForumsManager.SavePost(this.Context.Site, User, forum, viewModel.Post);
			await this.ForumsManager.SavePostTracking(viewModel.Post, HttpContext.User);
			await this.ForumsManager.Subscribe(viewModel.Post, HttpContext.User);

			return await BuildViewForumView(forum.Id, new());

		}

		[HttpPost]
		public async Task<ActionResult> SetForumPostStatus(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);

			if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_EDIT_POST))
			{
				return Forbid();
			}

			await this.ForumsManager.SetForumPostStatus(viewModel.Post, viewModel.Post.Status);

			return View("ViewPost", await BuildPostViewModel(viewModel.Forum.Id, viewModel.Post.Id, true));
		}

		[HttpPost]
		public async Task<ActionResult> SubscribeForum(ViewModels.ViewForum viewModel)
		{
			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);

			if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_SUBSCRIBE))
			{
				// subscribe to the forum 
				await this.ForumsManager.Subscribe(forum, HttpContext.User);
			}
			else
			{
				return BadRequest();
			}

			return await BuildViewForumView(forum.Id, viewModel.Posts);
		}


		[HttpPost]
		public async Task<ActionResult> UnSubscribeForum(ViewModels.ViewForum viewModel)
		{
			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);

			// Unsubscribe from the forum 
			await this.ForumsManager.UnSubscribe(forum, HttpContext.User);

			return await BuildViewForumView(forum.Id, viewModel.Posts);
		}

		[HttpPost]
		public async Task<ActionResult> ManageUnSubscribeForum(ViewModels.ManageSubscriptions viewModel, Guid forumId)
		{
			Models.Forum forum = await this.ForumsManager.Get(forumId);

			// Unsubscribe from the forum 
			await this.ForumsManager.UnSubscribe(forum, HttpContext.User);

			return View("ManageSubscriptions", await BuildSubscriptionsViewModel());
		}


		[HttpPost]
		public async Task<ActionResult> SubscribePost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);

			if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_SUBSCRIBE))
			{
				// subscribe to the forum post
				await this.ForumsManager.Subscribe(viewModel.Post, HttpContext.User);
			}
			else
			{
				return BadRequest();
			}

			return await ViewPost(viewModel.Post.Id);
		}

		[HttpPost]
		public async Task<ActionResult> UnSubscribePost(ViewModels.ViewForumPost viewModel)
		{
			// Unsubscribe from the forum post
			await this.ForumsManager.UnSubscribe(viewModel.Post, HttpContext.User);

			return await ViewPost(viewModel.Post.Id);
		}

		[HttpPost]
		public async Task<ActionResult> ManageUnSubscribePost(ViewModels.ManageSubscriptions viewModel, Guid forumPostId)
		{
			Post post = await this.ForumsManager.GetForumPost(forumPostId);

			// Unsubscribe from the forum post
			await this.ForumsManager.UnSubscribe(post, HttpContext.User);

			return View("ManageSubscriptions", await BuildSubscriptionsViewModel());
		}

		[HttpPost]
		public async Task<ActionResult> ApproveForumPost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE))
				{
					return Forbid();
				}
			}

			await this.ForumsManager.ApproveForumPost(viewModel.Post, true);

			return await BuildViewForumView(forum.Id, new());
		}


		[HttpPost]
		public async Task<ActionResult> RejectForumPost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE))
				{
					return Forbid();
				}
			}

			await this.ForumsManager.RejectForumPost(forum, viewModel.Post);

			return await BuildViewForumView(forum.Id, new());
		}

		[HttpPost]
		public async Task<ActionResult> PinForumPost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_PIN_POST))
				{
					return Forbid();
				}
			}

			await this.ForumsManager.PinForumPost(viewModel.Post, true);

			return await BuildViewForumView(forum.Id, new());
		}

		[HttpPost]
		public async Task<ActionResult> UnPinForumPost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_PIN_POST))
				{
					return Forbid();
				}
			}

			await this.ForumsManager.PinForumPost(viewModel.Post, false);

			return await BuildViewForumView(forum.Id, new());
		}

		[HttpPost]
		public async Task<ActionResult> LockForumPost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_LOCK_POST))
				{
					return Forbid();
				}
			}

			await this.ForumsManager.LockForumPost(viewModel.Post, true);

			return await BuildViewForumView(forum.Id, new());
		}

		[HttpPost]
		public async Task<ActionResult> UnlockForumPost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_LOCK_POST))
				{
					return Forbid();
				}
			}

			await this.ForumsManager.LockForumPost(viewModel.Post, false);

			return await BuildViewForumView(forum.Id, new());
		}

		[HttpPost]
		public async Task<ActionResult> DeleteForumPost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_DELETE_POST))
				{
					return Forbid();
				}
			}

			await this.ForumsManager.DeleteForumPost(forum, viewModel.Post);

			return await BuildViewForumView(forum.Id, new());
		}


		[HttpGet]
		public async Task<ActionResult> ReplyPost(Guid id)
		{
			ViewModels.ReplyForumPost viewModel = await BuildReplyPostViewModel(id, Guid.Empty);

			if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_REPLY_POST))
			{
				return Forbid();
			}

			return View("ReplyPost", viewModel);
		}

		[HttpGet]
		public async Task<ActionResult> EditForumPostReply(Guid postId, Guid replyId)
		{
			ViewModels.ReplyForumPost viewModel = await BuildReplyPostViewModel(postId, replyId);

			if (!(this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_REPLY_POST)) && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_EDIT_POST))
			{
				return Forbid();
			}

			return View("ReplyPost", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> SaveForumPostReply(ViewModels.ReplyForumPost viewModel)
		{
			// Remove modelstate for properties which are not validated for replies
			ModelState.RemovePrefix("Forum");
			ModelState.RemovePrefix("Post");
			ModelState.RemovePrefix("Reply.Id");

			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);
			Models.Post post = await this.ForumsManager.GetForumPost(viewModel.Post.Id);

			if (!this.ForumsManager.CheckPermission(this.Context.Site, User, forum, ForumsManager.PermissionScopes.FORUM_REPLY_POST))
			{
				return Forbid();
			}

			await this.ForumsManager.SavePostReply(this.Context.Site, User, post, viewModel.Reply);
			await this.ForumsManager.Subscribe(post, User);

			return await ViewPost(post.Id);
		}

		[HttpPost]
		public async Task<ActionResult> ApproveForumPostReply(ViewModels.ViewForumPost viewModel, Guid replyId)
		{
			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);
			Models.Reply reply = await this.ForumsManager.GetForumPostReply(replyId);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE))
				{
					return Forbid();
				}
			}

			await this.ForumsManager.ApproveForumPostReply(reply, true);

			return await ViewPost(viewModel.Post.Id);
		}


		[HttpPost]
		public async Task<ActionResult> RejectForumPostReply(ViewModels.ViewForumPost viewModel, Guid replyId)
		{
			Models.Forum forum = await this.ForumsManager.Get(viewModel.Forum.Id);
			Models.Reply reply = await this.ForumsManager.GetForumPostReply(replyId);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE))
				{
					return Forbid();
				}
			}

			await this.ForumsManager.RejectForumPostReply(reply);

			return await ViewPost(viewModel.Post.Id);
		}


		[HttpPost]
		public async Task<ActionResult> DeleteForumPostReply(Guid replyId)
		{
			Models.Reply reply = await this.ForumsManager.GetForumPostReply(replyId);

			if (reply == null)
			{
				return BadRequest();
			}

			Models.Post post = await this.ForumsManager.GetForumPost(reply.Post.Id);

			if (post == null)
			{
				return BadRequest();
			}

			Models.Forum forum = await this.ForumsManager.Get(post.ForumId);

			if (forum == null)
			{
				return BadRequest();
			}

			if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_REPLY_POST))
			{
				return Forbid();
			}

			await this.ForumsManager.DeletePostReply(forum, reply);

			return await ViewPost(post.Id);
		}

		/// <summary>
		/// Create a "list forums" viewmodel and populate it
		/// </summary>
		/// <returns></returns>
		private async Task<ViewModels.ListForums> BuildListForumsViewModel()
		{
			ViewModels.ListForums viewModel = new();

			List<Models.Group> groups = new();


			foreach (Models.Group group in await this.GroupsManager.List(this.Context.Module))
			{
				List<Models.Forum> forums = new();

				// only add forums if the user has view rights
				foreach (Models.Forum item in await this.ForumsManager.List(group))
				{
					// get a fully-populated forum object (will generally be in cache)
					Models.Forum forum = await this.ForumsManager.Get(item.Id);

					if (forum.EffectiveSettings().Visible)
					{
						if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_VIEW))
						{
							forums.Add(forum);
						}
					}
				}

				group.Forums = forums;

				if (group.Forums.Count > 0)
				{
					groups.Add(group);
				}
			}

			viewModel.Page = this.Context.Page;
			viewModel.Groups = groups;

			return viewModel;
		}

		/// <summary>
		/// Create a "view forum" viewmodel and populate it
		/// </summary>
		/// <returns></returns>
		private async Task<ViewModels.ViewForum> BuildViewForumViewModel(Guid forumId, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
		{
			ViewModels.ViewForum viewModel = new();

			viewModel.Page = this.Context.Page;

			// get a fully-populated forum object
			Models.Forum forum = await this.ForumsManager.Get(forumId);

			if (forum != null)
			{
				if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_VIEW))
				{
					viewModel.Forum = forum;

					if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE))
					{
						viewModel.Posts = await this.ForumsManager.ListPosts(forum, HttpContext.User, pagingSettings, Models.FlagStates.IsAny);
					}
					else
					{
						viewModel.Posts = await this.ForumsManager.ListPosts(forum, HttpContext.User, pagingSettings, Models.FlagStates.IsTrue);
					}

					viewModel.Subscription = await this.ForumsManager.GetSubscription(forum, HttpContext.User);
					viewModel.CanPost = forum.EffectiveSettings().Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_CREATE_POST);
					viewModel.CanSubscribe = forum.EffectiveSettings().Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_SUBSCRIBE);
				}
				else
				{
					throw new UnauthorizedAccessException();
				}
			}

			return viewModel;
		}

		/// <summary>
		/// Create a "view post" viewmodel and populate it
		/// </summary>
		/// <returns></returns>
		private async Task<ViewModels.ViewForumPost> BuildPostViewModel(Guid forumId, Guid forumPostId, Boolean readReplies)
		{
			Models.Forum forum;
			Models.Post post;
			Models.PostSubscription subscription;

			if (forumPostId != Guid.Empty)
			{
				post = await this.ForumsManager.GetForumPost(forumPostId);
				forum = await this.ForumsManager.Get(post.ForumId);
				subscription = await this.ForumsManager.GetSubscription(post, HttpContext.User);
			}
			else
			{
				forum = await this.ForumsManager.Get(forumId);
				post = new();
				subscription = null;
			}

			// Check forum (view) permissions			
			if (forum != null)
			{
				if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_VIEW))
				{
					return await BuildPostViewModel(new ViewModels.ViewForumPost
					{
						Forum = forum,
						Post = post,
						Subscription = subscription,
						Page = this.Context.Page
					}, readReplies);
				}
			}

			// if permission check failed, return an empty viewmodel to prevent successful editing
			return new();
		}

		private async Task<ViewModels.ViewForumPost> BuildPostViewModel(ViewModels.ViewForumPost viewModel, Boolean readReplies)
		{
			viewModel.Page = this.Context.Page;

			if (viewModel.Post != null)
			{
				// the data provider only populates some properties for attachments, get full File object(s)
				foreach (Models.Attachment attachment in viewModel.Post.Attachments)
				{
					attachment.File = await this.FileSystemManager.GetFile(this.Context.Site, attachment.File.Id);
				}
			}

			viewModel.CanEditPost = CanEditPost(viewModel.Forum, viewModel.Post);
			viewModel.CanReply = CanReplyPost(viewModel.Forum, viewModel.Post);
			viewModel.CanAttach = viewModel.Forum.EffectiveSettings().AllowAttachments && viewModel.Forum.EffectiveSettings().AttachmentsFolder != null && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_ATTACH_POST);
			viewModel.CanPinPost = viewModel.Forum.EffectiveSettings().Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_PIN_POST);
			viewModel.CanLockPost = viewModel.Forum.EffectiveSettings().Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_LOCK_POST);
			viewModel.CanDeletePost = CanDeletePost(viewModel.Forum, viewModel.Post);
			viewModel.CanSubscribe = viewModel.Forum.EffectiveSettings().Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_SUBSCRIBE);
			viewModel.CanApprovePost = viewModel.Forum.EffectiveSettings().Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_MODERATE);
			viewModel.Replies = readReplies ? await ListReplies(viewModel.Forum, viewModel.Post) : new List<Models.Reply>();

			if (viewModel.Replies != null)
      {
				foreach(Reply reply in viewModel.Replies)
        {
					reply.CanEditReply = CanEditReply(viewModel.Forum, reply);
					reply.CanDeleteReply = CanDeleteReply(viewModel.Forum, reply);
				}
			}
			return viewModel;
		}


		private async Task<IList<Models.Reply>> ListReplies(Models.Forum forum, Models.Post post)
		{
			IList<Models.Reply> results;

			if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE))
			{
				results = await this.ForumsManager.ListPostReplies(this.Context.Site, post, HttpContext.User, Models.FlagStates.IsAny);
			}
			else
			{
				results = await this.ForumsManager.ListPostReplies(this.Context.Site, post, HttpContext.User, Models.FlagStates.IsTrue);
			}

			foreach (Models.Reply reply in results)
			{
				if (reply != null)
				{
					// the data provider only populates the Id property for attachments, get full File object(s)
					foreach (Models.Attachment attachment in reply.Attachments)
					{
						attachment.File = await this.FileSystemManager.GetFile(this.Context.Site, attachment.File.Id);
					}
				}
			}

			return results;
		}

		/// <summary>
		/// Return a value indicating whether the current user can edit a post.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		/// <remarks>
		/// System administrators and site administrators can always edit posts.  Other users can edit posts if they have "forum post edit" permissions
		/// and are the post author.
		/// </remarks>
		private Boolean CanEditPost(Models.Forum forum, Models.Post post)
		{
			if (!forum.EffectiveSettings().Enabled) return false;

			if (HttpContext.User.IsSystemAdministrator() || HttpContext.User.IsSiteAdmin(this.Context.Site))
			{
				return true;
			}
			else
			{
				if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_EDIT_POST))
				{
					return (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE) || post.AddedBy == HttpContext.User.GetUserId());
				}
			}

			return false;
		}

		private Boolean CanDeletePost(Models.Forum forum, Models.Post post)
    {
			if (!forum.EffectiveSettings().Enabled) return false;

			if (HttpContext.User.IsSystemAdministrator() || HttpContext.User.IsSiteAdmin(this.Context.Site))
			{
				return true;
			}
			else
			{
				if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_DELETE_POST))
				{
					return (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE) || post.AddedBy == HttpContext.User.GetUserId());
				}
			}

			return false;
		}

		/// <summary>
		/// Return a value indicating whether the current user can edit a post.
		/// </summary>
		/// <param name="forum"></param>
		/// <param name="post"></param>
		/// <returns></returns>
		/// <remarks>
		/// System administrators and site administrators can always edit posts.  Other users can edit posts if they have "reply" permissions.
		/// The post author can always reply to their own post.
		/// </remarks>
		private Boolean CanReplyPost(Models.Forum forum, Models.Post post)
		{
			if (!forum.EffectiveSettings().Enabled) return false;

			if (HttpContext.User.IsSystemAdministrator() || HttpContext.User.IsSiteAdmin(this.Context.Site))
			{
				return true;
			}
			else
			{
				return (post.AddedBy == HttpContext.User.GetUserId()) || (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_REPLY_POST));
			}
		}

		private Boolean CanEditReply(Models.Forum forum, Models.Reply reply)
		{
			if (!forum.EffectiveSettings().Enabled) return false;

			if (HttpContext.User.IsSystemAdministrator() || HttpContext.User.IsSiteAdmin(this.Context.Site))
			{
				return true;
			}
			else
			{
				if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_EDIT_POST))
				{
					return (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE) || reply.AddedBy == HttpContext.User.GetUserId());
				}
			}

			return false;
		}

		private Boolean CanDeleteReply(Models.Forum forum, Models.Reply reply)
		{
			if (!forum.EffectiveSettings().Enabled) return false;

			if (HttpContext.User.IsSystemAdministrator() || HttpContext.User.IsSiteAdmin(this.Context.Site))
			{
				return true;
			}
			else
			{
				if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_DELETE_POST))
				{
					return (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE) || reply.AddedBy == HttpContext.User.GetUserId());
				}
			}

			return false;
		}

		/// <summary>
		/// Create a "view forum" viewmodel and populate it
		/// </summary>
		/// <returns></returns>
		private async Task<ViewModels.ReplyForumPost> BuildReplyPostViewModel(Guid forumPostId, Guid replyId)
		{
			Models.Forum forum;
			Models.Post post;
			Models.Reply reply = null;

			post = await this.ForumsManager.GetForumPost(forumPostId);
			forum = await this.ForumsManager.Get(post.ForumId);

			if (replyId != Guid.Empty)
			{
				reply = await this.ForumsManager.GetForumPostReply(replyId);
			}

			if (reply != null)
			{
				// the data provider only populates the Id property for attachments, get full File object(s)
				foreach (Models.Attachment attachment in reply.Attachments)
				{
					attachment.File = await this.FileSystemManager.GetFile(this.Context.Site, attachment.File.Id);
				}
			}

			// Check forum (view) permissions			
			if (forum != null && forum.EffectiveSettings().Enabled)
			{
				if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_VIEW))
				{
					return new ViewModels.ReplyForumPost
					{
            Page = this.Context.Page,
            Forum = forum,
						Post = post,
						Reply = reply,
						CanAttach = this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_ATTACH_POST),
						CanSubscribe = forum.EffectiveSettings().Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_SUBSCRIBE)
					};
				}
			}

			// if permission check failed, return an empty viewmodel to prevent successful editing
			return new();
		}

		private async Task<ViewModels.ManageSubscriptions> BuildSubscriptionsViewModel()
		{
			ViewModels.ManageSubscriptions manageSubscriptions = new();
			manageSubscriptions.Subscriptions = await this.ForumsManager.GetUserSubscriptions(User);
			return manageSubscriptions;
		}
	}
}