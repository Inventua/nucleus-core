using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Core;
using Nucleus.Core.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.Controllers
{
	[Extension("Forums")]
	public class ForumsController : Controller
	{
		private Context Context { get; }
		private PageModuleManager PageModuleManager { get; }
		private GroupsManager GroupsManager { get; }
		private ForumsManager ForumsManager { get; }
		private MailTemplateManager MailTemplateManager { get; }
		private RoleManager RoleManager { get; }
		private FileSystemManager FileSystemManager { get; }


		public ForumsController(IWebHostEnvironment webHostEnvironment, Context Context, PageModuleManager pageModuleManager, GroupsManager groupsManager, ForumsManager forumsManager, MailTemplateManager mailTemplateManager, RoleManager roleManager, FileSystemManager fileSystemManager)
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
		public ActionResult Index(Guid postId)
		{
			if (String.IsNullOrEmpty(this.Context.Parameters))
			{
				// display forum table of contents
				return View("ListForums", BuildListForumsViewModel());
			}
			else
			{
				if (postId == Guid.Empty)
				{
					// display selected forum
					Models.Forum forum = this.GroupsManager.FindForum(this.Context.Module, this.Context.Parameters);
					if (forum != null)
					{
						return View("ViewForum", BuildViewForumViewModel(forum.Id));
					}
					else
					{
						return NotFound();
					}
				}
				else
				{
					// display selected post
					return EditPost(postId);
				}

			}
		}

		[HttpGet]
		public ActionResult AddPost(Guid forumId)
		{
			ViewModels.ViewForumPost viewModel = BuildPostViewModel(forumId, Guid.Empty, false);

			if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_CREATE_POST))
			{
				return View("EditPost", viewModel);
			}
			else
			{
				return Unauthorized();
			}
		}

		[HttpGet]
		public ActionResult EditPost(Guid id)
		{
			ViewModels.ViewForumPost viewModel = BuildPostViewModel(Guid.Empty, id, false);

			if (CanEditPost(viewModel.Forum, viewModel.Post))
			{
				return View("EditPost", viewModel);
			}
			else
			{
				return Unauthorized();
			}
		}

		[HttpPost]
		public async Task<ActionResult> AddPostAttachment(ViewModels.ViewForumPost viewModel, [FromForm] IFormFile mediaFile)
		{
			if (mediaFile != null)
			{
				viewModel.Forum = this.ForumsManager.Get(viewModel.Forum.Id);
				viewModel.Forum.AttachmentsFolder = this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Forum.AttachmentsFolder.Id);
				using (System.IO.Stream fileStream = mediaFile.OpenReadStream())
				{
					Models.Attachment attachment = new() 
					{ 
						 File = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.Forum.AttachmentsFolder.Provider, viewModel.Forum.AttachmentsFolder.Path, mediaFile.FileName, fileStream, false)
					};

					viewModel.Post.Attachments.Add(attachment);
				}
			}
			else
			{
				return BadRequest();
			}

			return View("EditPost", viewModel);
		}

		[HttpPost]
		public ActionResult DeletePostAttachment(ViewModels.ViewForumPost viewModel, Guid id)
		{
			viewModel.Forum = this.ForumsManager.Get(viewModel.Forum.Id);

			Models.Attachment attachment = viewModel.Post.Attachments.Where(attachment => attachment.Id == id).FirstOrDefault();
			if (attachment != null)
			{
				viewModel.Post.Attachments.Remove(attachment);
			}
			else
			{
				return BadRequest();
			}

			return View("EditPost", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> AddReplyAttachment(ViewModels.ReplyForumPost viewModel, [FromForm] IFormFile mediaFile)
		{
			if (mediaFile != null)
			{
				viewModel.Forum = this.ForumsManager.Get(viewModel.Forum.Id);
				viewModel.Forum.AttachmentsFolder = this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Forum.AttachmentsFolder.Id);
				using (System.IO.Stream fileStream = mediaFile.OpenReadStream())
				{
					Models.Attachment attachment = new()
					{
						File = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.Forum.AttachmentsFolder.Provider, viewModel.Forum.AttachmentsFolder.Path, mediaFile.FileName, fileStream, false)
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
		public ActionResult DeleteReplyAttachment(ViewModels.ReplyForumPost viewModel, Guid id)
		{
			viewModel.Forum = this.ForumsManager.Get(viewModel.Forum.Id);

			Models.Attachment attachment = viewModel.Reply.Attachments.Where(attachment=>attachment.Id == id).FirstOrDefault();
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
		public ActionResult ViewPost(Guid id)
		{
			ViewModels.ViewForumPost viewModel = BuildPostViewModel(Guid.Empty, id, true);

			if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_VIEW))
			{
				return View("ViewPost", viewModel);
			}
			else
			{
				return Unauthorized();
			}
		}

		[HttpPost]
		public ActionResult SaveForumPost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = this.ForumsManager.Get(viewModel.Forum.Id);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_CREATE_POST))
				{
					return Unauthorized();
				}
			}
			else if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_EDIT_POST))
			{
				return Unauthorized();
			}

			// make sure that pinned, locked status hasn't been set by users who don't have rights
			if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_PIN_POST))
			{
				if (viewModel.Post.IsPinned)
				{
					return Unauthorized();
				}
			}

			if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_LOCK_POST))
			{
				if (viewModel.Post.IsLocked)
				{
					return Unauthorized();
				}
			}
						
			this.ForumsManager.SavePost(this.Context.Site, forum, viewModel.Post);

			return View("ViewForum", BuildViewForumViewModel(forum.Id));
		}

		[HttpPost]
		public ActionResult SetForumPostStatus(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = this.ForumsManager.Get(viewModel.Forum.Id);

			if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_EDIT_POST))
			{
				return Unauthorized();
			}

			this.ForumsManager.SetForumPostStatus(viewModel.Post, viewModel.Post.Status);

			return View("ViewPost", BuildPostViewModel(viewModel.Forum.Id, viewModel.Post.Id, true));
		}

		[HttpPost]
		public ActionResult SubscribeForum(ViewModels.ViewForum viewModel)
		{
			Models.Forum forum = this.ForumsManager.Get(viewModel.Forum.Id);

			// TODO
			if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_SUBSCRIBE))
			{
				// subscribe to the forum 

			}
			else
			{
				return BadRequest();
			}

			return View("ViewForum", BuildViewForumViewModel(forum.Id));
		}

		[HttpPost]
		public ActionResult SubscribePost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = this.ForumsManager.Get(viewModel.Forum.Id);

			// TODO
			if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_SUBSCRIBE))
			{
				// subscribe to the forum post

			}
			else
			{
				return BadRequest();
			}

			return View("ViewForum", BuildViewForumViewModel(forum.Id));
		}

		[HttpPost]
		public ActionResult ApproveForumPost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = this.ForumsManager.Get(viewModel.Forum.Id);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE))
				{
					return Unauthorized();
				}
			}

			this.ForumsManager.ApproveForumPost(viewModel.Post, true);

			return View("ViewForum", BuildViewForumViewModel(forum.Id));
		}

		[HttpPost]
		public ActionResult RejectForumPost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = this.ForumsManager.Get(viewModel.Forum.Id);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE))
				{
					return Unauthorized();
				}
			}

			this.ForumsManager.RejectForumPost(viewModel.Forum, viewModel.Post);

			return View("ViewForum", BuildViewForumViewModel(forum.Id));
		}

		[HttpPost]
		public ActionResult PinForumPost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = this.ForumsManager.Get(viewModel.Forum.Id);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_PIN_POST))
				{
					return Unauthorized();
				}
			}

			this.ForumsManager.PinForumPost(viewModel.Post, true);

			return View("ViewForum", BuildViewForumViewModel(forum.Id));
		}

		[HttpPost]
		public ActionResult LockForumPost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = this.ForumsManager.Get(viewModel.Forum.Id);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_LOCK_POST))
				{
					return Unauthorized();
				}
			}

			this.ForumsManager.LockForumPost(viewModel.Post, true);

			return View("ViewForum", BuildViewForumViewModel(forum.Id));
		}

		[HttpPost]
		public ActionResult DeleteForumPost(ViewModels.ViewForumPost viewModel)
		{
			Models.Forum forum = this.ForumsManager.Get(viewModel.Forum.Id);

			if (viewModel.Post.Id == Guid.Empty)
			{
				if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_DELETE_POST))
				{
					return Unauthorized();
				}
			}

			this.ForumsManager.DeleteForumPost(forum, viewModel.Post);

			return View("ViewForum", BuildViewForumViewModel(forum.Id));
		}


		[HttpGet]
		public ActionResult ReplyPost(Guid id)
		{
			ViewModels.ReplyForumPost viewModel = BuildReplyPostViewModel(id, Guid.Empty);

			if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_REPLY_POST))
			{
				return Unauthorized();
			}

			return View("ReplyPost", viewModel);
		}

		[HttpGet]
		public ActionResult EditForumPostReply(Guid postId, Guid replyId)
		{
			ViewModels.ReplyForumPost viewModel = BuildReplyPostViewModel(postId, replyId);

			if (!(this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_REPLY_POST)) && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, viewModel.Forum, ForumsManager.PermissionScopes.FORUM_EDIT_POST))
			{
				return Unauthorized();
			}

			return View("ReplyPost", viewModel);
		}

		[HttpPost]
		public ActionResult SaveForumPostReply(ViewModels.ReplyForumPost viewModel)
		{
			Models.Forum forum = this.ForumsManager.Get(viewModel.Forum.Id);
			Models.Post post = this.ForumsManager.GetForumPost(viewModel.Post.Id);

			if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_REPLY_POST))
			{
				return Unauthorized();
			}

			this.ForumsManager.SavePostReply(this.Context.Site, post, viewModel.Reply);

			return ViewPost(viewModel.Post.Id);
		}

		[HttpPost]
		public ActionResult DeleteForumPostReply(Guid replyId)
		{
			Models.Reply reply = this.ForumsManager.GetForumPostReply(replyId);

			if (reply == null)
			{
				return BadRequest();
			}

			Models.Post post = this.ForumsManager.GetForumPost(reply.ForumPostId);

			if (post == null)
			{
				return BadRequest();
			}

			Models.Forum forum = this.ForumsManager.Get(post.ForumId);

			if (forum == null)
			{
				return BadRequest();
			}

			if (!this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_REPLY_POST))
			{
				return Unauthorized();
			}

			this.ForumsManager.DeletePostReply(forum, reply);

			return ViewPost(post.Id);
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public ActionResult Settings(ViewModels.GroupSettings viewModel)
		{
			return View("Settings", BuildSettingsViewModel());
		}

		/// <summary>
		/// Create a "list forums" viewmodel and populate it
		/// </summary>
		/// <returns></returns>
		private ViewModels.ListForums BuildListForumsViewModel()
		{
			ViewModels.ListForums viewModel = new();

			List<Models.Group> groups = new();


			foreach (Models.Group group in this.GroupsManager.List(this.Context.Module))
			{
				List<Models.Forum> forums = new();

				// only add forums if the user has view rights
				foreach (Models.Forum item in this.ForumsManager.List(group))
				{
					// get a fully-populated forum object
					Models.Forum forum = this.ForumsManager.Get(item.Id);

					if (forum.Settings.Visible)
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

			viewModel.Groups = groups;
			//viewModel.ShowSize = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SIZE, true);
			return viewModel;
		}

		/// <summary>
		/// Create a "view forum" viewmodel and populate it
		/// </summary>
		/// <returns></returns>
		private ViewModels.ViewForum BuildViewForumViewModel(Guid forumId)
		{
			ViewModels.ViewForum viewModel = new();

			// get a fully-populated forum object
			Models.Forum forum = this.ForumsManager.Get(forumId);

			if (forum != null)
			{
				if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_VIEW))
				{
					viewModel.Forum = forum;

					if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE))
					{
						viewModel.Posts = this.ForumsManager.ListPosts(forum, Models.FlagStates.IsAny);
					}
					else
					{
						viewModel.Posts = this.ForumsManager.ListPosts(forum, Models.FlagStates.IsTrue);
					}

					viewModel.CanPost = forum.Settings.Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_CREATE_POST);
					viewModel.CanSubscribe = forum.Settings.Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_SUBSCRIBE);										
				}
			}

			return viewModel;
		}


		/// <summary>
		/// Create a "view forum" viewmodel and populate it
		/// </summary>
		/// <returns></returns>
		private ViewModels.ViewForumPost BuildPostViewModel(Guid forumId, Guid forumPostId, Boolean readReplies)
		{
			Models.Forum forum;
			Models.Post post;

			if (forumPostId != Guid.Empty)
			{
				post = this.ForumsManager.GetForumPost(forumPostId);
				forum = this.ForumsManager.Get(post.ForumId);
			}
			else
			{
				forum = this.ForumsManager.Get(forumId);
				post = new();
			}

			if (post != null)
			{
				// the data provider only populates the Id property for attachments, get full File object(s)
				foreach (Models.Attachment attachment in post.Attachments)
				{
					attachment.File = this.FileSystemManager.GetFile(this.Context.Site,attachment.File.Id);
				}
			}

			// Check forum (view) permissions			
			if (forum != null)
			{
				if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_VIEW))
				{
					return new ViewModels.ViewForumPost
					{
						Forum = forum,
						Post = post,
						CanEditPost = CanEditPost(forum, post),
						CanReply = CanReplyPost(forum, post),
						CanAttach = forum.Settings.Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_ATTACH_POST),
						CanPinPost = forum.Settings.Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_PIN_POST),
						CanLockPost = forum.Settings.Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_LOCK_POST),
						CanDeletePost = forum.Settings.Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_DELETE_POST),
						CanSubscribe = forum.Settings.Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_SUBSCRIBE),
						CanApprovePost = forum.Settings.Enabled && this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE),
						Replies = readReplies ? ListReplies(forum, post) : new List<Models.Reply>()
					};
				}
			}

			// if permission check failed, return an empty viewmodel to prevent successful editing
			return new();
		}

		private IList<Models.Reply> ListReplies(Models.Forum forum, Models.Post post)
		{
			IList<Models.Reply> results;

			if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_MODERATE))
			{
				results = this.ForumsManager.ListPostReplies(post, Models.FlagStates.IsAny);
			}
			else
			{
				results = this.ForumsManager.ListPostReplies(post, Models.FlagStates.IsTrue);
			}

			foreach (Models.Reply reply in results)
			{
				if (reply != null)
				{
					// the data provider only populates the Id property for attachments, get full File object(s)
					foreach (Models.Attachment attachment in reply.Attachments)
					{
						attachment.File = this.FileSystemManager.GetFile(this.Context.Site, attachment.File.Id);
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
			if (!forum.Settings.Enabled) return false; 

			if (HttpContext.User.IsSystemAdministrator() || HttpContext.User.IsSiteAdmin(this.Context.Site))
			{
				return true;
			}
			else
			{
				if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_EDIT_POST))
				{
					return (post.AddedBy == HttpContext.User.GetUserId());
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
			if (!forum.Settings.Enabled) return false;

			if (HttpContext.User.IsSystemAdministrator() || HttpContext.User.IsSiteAdmin(this.Context.Site))
			{
				return true;
			}
			else
			{
				return (post.AddedBy == HttpContext.User.GetUserId()) || (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_REPLY_POST));
			}
		}

		/// <summary>
		/// Create a "view forum" viewmodel and populate it
		/// </summary>
		/// <returns></returns>
		private ViewModels.ReplyForumPost BuildReplyPostViewModel(Guid forumPostId, Guid replyId)
		{
			Models.Forum forum;
			Models.Post post;
			Models.Reply reply = null;

			post = this.ForumsManager.GetForumPost(forumPostId);
			forum = this.ForumsManager.Get(post.ForumId);

			if (replyId != Guid.Empty)
			{
				reply = this.ForumsManager.GetForumPostReply(replyId);
			}

			if (reply != null)
			{
				// the data provider only populates the Id property for attachments, get full File object(s)
				foreach (Models.Attachment attachment in reply.Attachments)
				{
					attachment.File = this.FileSystemManager.GetFile(this.Context.Site, attachment.File.Id);
				}
			}

			// Check forum (view) permissions			
			if (forum != null && forum.Settings.Enabled)
			{
				if (this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_VIEW))
				{
					return new ViewModels.ReplyForumPost
					{
						Forum = forum,
						Post = post,
						Reply = reply,
						CanAttach = this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_ATTACH_POST),
						CanSubscribe = this.ForumsManager.CheckPermission(this.Context.Site, HttpContext.User, forum, ForumsManager.PermissionScopes.FORUM_SUBSCRIBE)
					};
				}
			}

			// if permission check failed, return an empty viewmodel to prevent successful editing
			return new();
		}

		/// <summary>
		/// Create a "settings" viewmodel and populate it
		/// </summary>
		/// <returns></returns>
		private ViewModels.Settings BuildSettingsViewModel()
		{
			ViewModels.Settings viewModel = new();
			viewModel.Groups = this.GroupsManager.List(this.Context.Module);
			return viewModel;
		}
	}
}