using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Nucleus.Abstractions.Models.Permissions;


namespace Nucleus.Modules.Forums.Controllers
{
	[Extension("Forums")]
	[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
	public class ForumsAdminController : Controller
	{
		private Context Context { get; }
		private PageModuleManager PageModuleManager { get; }
		private GroupsManager GroupsManager { get; }
		private ForumsManager ForumsManager { get; }
		private MailTemplateManager MailTemplateManager { get; }
		private RoleManager RoleManager { get; }
		private FileSystemManager FileSystemManager { get; }
		private ListManager ListManager { get; }

		public ForumsAdminController(IWebHostEnvironment webHostEnvironment, Context Context, PageModuleManager pageModuleManager, GroupsManager groupsManager, ForumsManager forumsManager, MailTemplateManager mailTemplateManager, RoleManager roleManager, ListManager listManager, FileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.GroupsManager = groupsManager;
			this.ForumsManager = forumsManager;
			this.MailTemplateManager = mailTemplateManager;
			this.RoleManager = roleManager;
			this.ListManager = listManager;
			FileSystemManager = fileSystemManager;
		}


		[HttpGet]
		[HttpPost]
		public ActionResult Settings(ViewModels.GroupSettings viewModel)
		{
			return View("Settings", BuildSettingsViewModel());
		}

		[HttpGet]
		public ActionResult NewGroup()
		{
			ViewModels.GroupSettings viewModel = BuildGroupSettingsViewModel(null);

			viewModel.Group = this.GroupsManager.Create();

			return View("GroupEditor", BuildGroupSettingsViewModel(viewModel));
		}

		[HttpGet]
		public ActionResult EditGroup(Guid id)
		{
			ViewModels.GroupSettings viewModel = BuildGroupSettingsViewModel(id);

			return View("GroupEditor", BuildGroupSettingsViewModel(viewModel));
		}

		[HttpPost]
		public ActionResult SelectGroupAttachmentsFolder(ViewModels.GroupSettings viewModel)
		{
			return View("GroupEditor", BuildGroupSettingsViewModel(viewModel));
		}

		[HttpPost]
		public ActionResult SaveGroup(ViewModels.GroupSettings viewModel)
		{
			//viewModel.Group.AttachmentsFolder = this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedFolder.Provider, viewModel.SelectedFolder.Path);
			viewModel.Group.Permissions = RebuildPermissions(viewModel.GroupPermissions);
			this.GroupsManager.Save(this.Context.Module, viewModel.Group);
			return View("Settings", BuildSettingsViewModel());
		}

		[HttpPost]
		public ActionResult DeleteGroup(ViewModels.GroupSettings viewModel)
		{
			this.GroupsManager.Delete(viewModel.Group);
			return Ok();
		}

		[HttpPost]
		public ActionResult AddGroupPermissionRole(ViewModels.GroupSettings viewModel)
		{
			if (viewModel.SelectedRoleId != Guid.Empty)
			{
				Role role = this.RoleManager.Get(viewModel.SelectedRoleId);
				viewModel.Group.Permissions = RebuildPermissions(viewModel.GroupPermissions);

				this.GroupsManager.CreatePermissions(viewModel.Group, role);
			}

			viewModel.GroupPermissions = viewModel.Group.Permissions.ToPermissionsList();

			return View("GroupEditor", BuildGroupSettingsViewModel(viewModel));

		}

		[HttpPost]
		public ActionResult DeleteGroupPermissionRole(ViewModels.GroupSettings viewModel, Guid id)
		{
			viewModel.Group.Permissions = RebuildPermissions(viewModel.GroupPermissions);

			foreach (Permission permission in viewModel.Group.Permissions.ToList())
			{
				if (permission.Role.Id == id)
				{
					viewModel.Group.Permissions.Remove(permission);
				}
			}

			viewModel.GroupPermissions = viewModel.Group.Permissions.ToPermissionsList();

			return View("GroupEditor", BuildGroupSettingsViewModel(viewModel));
		}

		[HttpPost]
		public ActionResult NewForum(ViewModels.GroupSettings viewModel)
		{
			ViewModels.ForumSettings forumSettings = BuildForumSettingsViewModel(null);
			forumSettings.GroupId = viewModel.Group.Id;

			return View("ForumEditor", forumSettings);
		}

		[HttpPost]
		public ActionResult AddForumPermissionRole(ViewModels.ForumSettings viewModel)
		{
			if (viewModel.SelectedRoleId != Guid.Empty)
			{
				Role role = this.RoleManager.Get(viewModel.SelectedRoleId);
				viewModel.Forum.Permissions = RebuildPermissions(viewModel.ForumPermissions);

				this.ForumsManager.CreatePermissions(viewModel.Forum, role);
			}

			viewModel.ForumPermissions = viewModel.Forum.Permissions.ToPermissionsList();

			return View("ForumEditor", BuildForumSettingsViewModel(viewModel));

		}

		[HttpPost]
		public ActionResult DeleteForumPermissionRole(ViewModels.ForumSettings viewModel, Guid id)
		{
			viewModel.Forum.Permissions = RebuildPermissions(viewModel.ForumPermissions);

			foreach (Permission permission in viewModel.Forum.Permissions.ToList())
			{
				if (permission.Role.Id == id)
				{
					viewModel.Forum.Permissions.Remove(permission);
				}
			}

			viewModel.ForumPermissions = viewModel.Forum.Permissions.ToPermissionsList();

			return View("ForumEditor", BuildForumSettingsViewModel(viewModel));
		}

		[HttpPost]
		public ActionResult EditForum(Guid id)
		{
			return View("ForumEditor", BuildForumSettingsViewModel(id));
		}

		[HttpPost]
		public ActionResult SelectForumAttachmentsFolder(ViewModels.ForumSettings viewModel)
		{
			return View("ForumEditor", BuildForumSettingsViewModel(viewModel));
		}

		[HttpPost]
		public ActionResult SaveForum(ViewModels.ForumSettings viewModel)
		{
			Models.Group group = this.GroupsManager.Get(viewModel.GroupId);

			//viewModel.Forum.AttachmentsFolder = this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedFolder.Provider, viewModel.SelectedFolder.Path);
			viewModel.Forum.Permissions = RebuildPermissions(viewModel.ForumPermissions);
			this.ForumsManager.Save(group, viewModel.Forum);
			return Ok();
		}

		[HttpPost]
		public ActionResult DeleteForum(ViewModels.ForumSettings viewModel, Guid id)
		{
			Models.Forum forum = this.ForumsManager.Get(id);

			this.ForumsManager.Delete(forum);
			return Ok();
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

		/// <summary>
		/// Create a "group settings" viewmodel and populate it
		/// </summary>
		/// <returns></returns>
		private ViewModels.GroupSettings BuildGroupSettingsViewModel(ViewModels.GroupSettings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.Groups = this.GroupsManager.List(this.Context.Module);
			viewModel.MailTemplates = this.MailTemplateManager.List(this.Context.Site);
			viewModel.AvailableRoles = this.RoleManager.List(this.Context.Site).Where(role=>role.Id != this.Context.Site.AdministratorsRole?.Id);
			viewModel.Lists = this.ListManager.List(this.Context.Site);

			viewModel.ForumPermissionTypes = this.GroupsManager.ListForumPermissionTypes();

			if (viewModel.Group != null)
			{
				viewModel.Group.Permissions = RebuildPermissions(viewModel.GroupPermissions);
			}

			return viewModel;
		}

		private ViewModels.GroupSettings BuildGroupSettingsViewModel(Guid id)
		{
			ViewModels.GroupSettings viewModel = BuildGroupSettingsViewModel(null);

			viewModel.Group = this.GroupsManager.Get(id);
			viewModel.GroupPermissions = viewModel.Group.Permissions.ToPermissionsList();
			//viewModel.SelectedFolder = viewModel.Group.AttachmentsFolder;

			return viewModel;
		}


		/// <summary>
		/// Create a "forum settings" viewmodel and populate it
		/// </summary>
		/// <returns></returns>
		private ViewModels.ForumSettings BuildForumSettingsViewModel(ViewModels.ForumSettings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.MailTemplates = this.MailTemplateManager.List(this.Context.Site);
			viewModel.AvailableRoles = this.RoleManager.List(this.Context.Site).Where(role => role.Id != this.Context.Site.AdministratorsRole?.Id);
			viewModel.Lists = this.ListManager.List(this.Context.Site);

			viewModel.ForumPermissionTypes = this.GroupsManager.ListForumPermissionTypes();

			if (viewModel.Forum != null)
			{
				viewModel.Forum.Permissions = RebuildPermissions(viewModel.ForumPermissions);
			}

			return viewModel;
		}

		private ViewModels.ForumSettings BuildForumSettingsViewModel(Guid id)
		{
			ViewModels.ForumSettings viewModel = BuildForumSettingsViewModel(null);

			viewModel.Forum = this.ForumsManager.Get(id);
			viewModel.ForumPermissions = viewModel.Forum.Permissions.ToPermissionsList();
			//viewModel.SelectedFolder = viewModel.Forum.AttachmentsFolder;

			return viewModel;
		}


		private List<Permission> RebuildPermissions(PermissionsList permissions)
		{
			if (permissions == null) return null;

			foreach (KeyValuePair<Guid, PermissionsListItem> rolePermissions in permissions)
			{
				foreach (Permission permission in rolePermissions.Value.Permissions)
				{
					permission.Role = this.RoleManager.Get(rolePermissions.Key);

					rolePermissions.Value.Role = permission.Role;
				}
			}

			return permissions.ToList();
		}
	}
}