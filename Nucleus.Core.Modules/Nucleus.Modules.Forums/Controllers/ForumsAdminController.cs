using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using Nucleus.Abstractions.Models.Permissions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Extensions;

namespace Nucleus.Modules.Forums.Controllers
{
	[Extension("Forums")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
	public class ForumsAdminController : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private GroupsManager GroupsManager { get; }
		private ForumsManager ForumsManager { get; }
		private IMailTemplateManager MailTemplateManager { get; }
		private IRoleManager RoleManager { get; }
		private IFileSystemManager FileSystemManager { get; }
		private IListManager ListManager { get; }

		public ForumsAdminController(IWebHostEnvironment webHostEnvironment, Context Context, IPageModuleManager pageModuleManager, GroupsManager groupsManager, ForumsManager forumsManager, IMailTemplateManager mailTemplateManager, IRoleManager roleManager, IListManager listManager, IFileSystemManager fileSystemManager)
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
		public async Task<ActionResult> Settings(ViewModels.GroupSettings viewModel)
		{
			return View("Settings", await BuildSettingsViewModel());
		}

		[HttpGet]
		public async Task<ActionResult> NewGroup()
		{
			ViewModels.GroupSettings viewModel = await BuildGroupSettingsViewModel(new ViewModels.GroupSettings(), false);

			viewModel.Group = await this.GroupsManager.Create();

			return View("GroupEditor", viewModel);
		}

		[HttpGet]
		public async Task<ActionResult> EditGroup(Guid id)
		{
			ViewModels.GroupSettings viewModel = await BuildGroupSettingsViewModel(id);

			return View("GroupEditor", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> SelectGroupAttachmentsFolder(ViewModels.GroupSettings viewModel)
		{
			return View("GroupEditor", await BuildGroupSettingsViewModel(viewModel, true));
		}

		[HttpPost]
		public async Task<ActionResult> SaveGroup(ViewModels.GroupSettings viewModel)
		{
			// Model binding/DropDownListFor sets a not-null/Id=Guid=Guid.Empty value for "not selected", we need to change this to null so we
			// don't get a foreign key constraint error
			if (viewModel.Group.Settings.StatusList?.Id == Guid.Empty)
			{
				viewModel.Group.Settings.StatusList = null;
			}

			viewModel.Group.Permissions = await RebuildPermissions(viewModel.GroupPermissions);
			await this.GroupsManager.Save(this.Context.Module, viewModel.Group);
			return View("Settings", await BuildSettingsViewModel());
		}

		[HttpPost]
		public async Task<ActionResult> DeleteGroup(ViewModels.GroupSettings viewModel)
		{
			await this.GroupsManager.Delete(viewModel.Group);
			return Ok();
		}

		[HttpPost]
		public async Task<ActionResult> AddGroupPermissionRole(ViewModels.GroupSettings viewModel)
		{
			if (viewModel.SelectedRoleId != Guid.Empty)
			{
				Role role = await this.RoleManager.Get(viewModel.SelectedRoleId);
				viewModel.Group.Permissions = await RebuildPermissions(viewModel.GroupPermissions);

				await this.GroupsManager.CreatePermissions(viewModel.Group, role);
			}

			viewModel.GroupPermissions = viewModel.Group.Permissions.ToPermissionsList(this.Context.Site);

			return View("GroupEditor", await BuildGroupSettingsViewModel (viewModel, true));

		}

		[HttpPost]
		public async Task<ActionResult> DeleteGroupPermissionRole(ViewModels.GroupSettings viewModel, Guid id)
		{
			viewModel.Group.Permissions = await RebuildPermissions(viewModel.GroupPermissions);

			foreach (Permission permission in viewModel.Group.Permissions.ToList())
			{
				if (permission.Role.Id == id)
				{
					viewModel.Group.Permissions.Remove(permission);
				}
			}

			viewModel.GroupPermissions = viewModel.Group.Permissions.ToPermissionsList(this.Context.Site);

			return View("GroupEditor", await BuildGroupSettingsViewModel(viewModel, true));
		}

		[HttpPost]
		public async Task<ActionResult> NewForum(ViewModels.GroupSettings viewModel)
		{
			ViewModels.ForumSettings forumSettings = new();

			forumSettings.Forum = await this.ForumsManager.CreateForum(viewModel.Group);
			forumSettings.GroupId = viewModel.Group.Id;

			await BuildForumSettingsViewModel(forumSettings, false);
			
			return View("ForumEditor", forumSettings);
		}

		[HttpPost]
		public async Task<ActionResult> AddForumPermissionRole(ViewModels.ForumSettings viewModel)
		{
			if (viewModel.SelectedRoleId != Guid.Empty)
			{
				Role role = await this.RoleManager.Get(viewModel.SelectedRoleId);
				viewModel.Forum.Permissions = await RebuildPermissions(viewModel.ForumPermissions);

				await this.ForumsManager.CreatePermissions(viewModel.Forum, role);
			}

			viewModel.ForumPermissions = viewModel.Forum.Permissions.ToPermissionsList(this.Context.Site);

			return View("ForumEditor", await BuildForumSettingsViewModel(viewModel, true));

		}

		[HttpPost]
		public async Task<ActionResult> DeleteForumPermissionRole(ViewModels.ForumSettings viewModel, Guid id)
		{
			viewModel.Forum.Permissions = await RebuildPermissions(viewModel.ForumPermissions);

			foreach (Permission permission in viewModel.Forum.Permissions.ToList())
			{
				if (permission.Role.Id == id)
				{
					viewModel.Forum.Permissions.Remove(permission);
				}
			}

			viewModel.ForumPermissions = viewModel.Forum.Permissions.ToPermissionsList(this.Context.Site);

			return View("ForumEditor", await BuildForumSettingsViewModel(viewModel, true));
		}

		[HttpPost]
		public async Task<ActionResult> EditForum(ViewModels.GroupSettings viewModel, Guid id)
		{
			ViewModels.ForumSettings forumSettings = await BuildForumSettingsViewModel(id);
			forumSettings.GroupId = viewModel.Group.Id;
			return View("ForumEditor", forumSettings);
		}

		[HttpPost]
		public async Task<ActionResult> SelectForumAttachmentsFolder(ViewModels.ForumSettings viewModel)
		{
			return View("ForumEditor", await BuildForumSettingsViewModel(viewModel, true));
		}

		[HttpPost]
		public async Task<ActionResult> SaveForum(ViewModels.ForumSettings viewModel)
		{
			Models.Group group = await this.GroupsManager.Get(viewModel.GroupId);

			// Model binding/DropDownListFor sets a not-null/Id=Guid=Guid.Empty value for "not selected", we need to change this to null so we
			// don't get a foreign key constraint error
			if (viewModel.Forum.Settings.StatusList?.Id == Guid.Empty)
			{
				viewModel.Forum.Settings.StatusList = null;
			}

			viewModel.Forum.Permissions = await RebuildPermissions(viewModel.ForumPermissions);
			await this.ForumsManager.Save(group, viewModel.Forum);
			
			return View("_ForumList", await BuildGroupSettingsViewModel(viewModel.GroupId));
		}
		
		[HttpPost]
		public async Task<ActionResult> DeleteForum(ViewModels.ForumSettings viewModel, Guid id, Guid groupId)
		{
			// delete button from the forum editor popup OR group editor forums list (which is why groupId is on the query string instead of in the view model)
			Models.Forum forum = await this.ForumsManager.Get(this.Context.Site, id);

			await this.ForumsManager.Delete(forum);
			
			return View("_ForumList", await BuildGroupSettingsViewModel (groupId));
		}


		/// <summary>
		/// Create a "settings" viewmodel and populate it
		/// </summary>
		/// <returns></returns>
		private async Task<ViewModels.Settings> BuildSettingsViewModel()
		{
			ViewModels.Settings viewModel = new();
			viewModel.Groups = await this.GroupsManager.List(this.Context.Module);
			return viewModel;
		}

		/// <summary>
		/// Create a "group settings" viewmodel and populate it
		/// </summary>
		/// <returns></returns>
		private async Task<ViewModels.GroupSettings> BuildGroupSettingsViewModel(ViewModels.GroupSettings viewModel, Boolean rebuildPermissions)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.Groups = await this.GroupsManager.List(this.Context.Module);
			viewModel.MailTemplates = await this.MailTemplateManager.List(this.Context.Site);
			viewModel.AvailableRoles = await GetAvailableRoles(viewModel.Group?.Permissions); 
			viewModel.Lists = await this.ListManager.List(this.Context.Site);

			viewModel.ForumPermissionTypes = await this.GroupsManager.ListForumPermissionTypes();

			if (rebuildPermissions)
			{
				viewModel.Group.Permissions = await RebuildPermissions(viewModel.GroupPermissions);
			}

			return viewModel;
		}

		private async Task<ViewModels.GroupSettings> BuildGroupSettingsViewModel(Guid id)
		{
			ViewModels.GroupSettings viewModel = new();

			viewModel.Group = await this.GroupsManager.Get(id);
			await BuildGroupSettingsViewModel(viewModel, false);
			
			viewModel.GroupPermissions = viewModel.Group.Permissions.ToPermissionsList(this.Context.Site);

			return viewModel;
		}


		/// <summary>
		/// Create a "forum settings" viewmodel and populate it
		/// </summary>
		/// <returns></returns>
		private async Task<ViewModels.ForumSettings> BuildForumSettingsViewModel(ViewModels.ForumSettings viewModel, Boolean rebuildPermissions)
		{
			viewModel.ForumPermissionTypes = await this.GroupsManager.ListForumPermissionTypes();
						
			if (rebuildPermissions)
			{
				viewModel.Forum.Permissions = await RebuildPermissions(viewModel.ForumPermissions);
			}

			viewModel.MailTemplates = await this.MailTemplateManager.List(this.Context.Site);
			viewModel.AvailableRoles = await GetAvailableRoles(viewModel.Forum.Permissions);
			viewModel.Lists = await this.ListManager.List(this.Context.Site);

			return viewModel;
		}

		private async Task<ViewModels.ForumSettings> BuildForumSettingsViewModel(Guid id)
		{
			ViewModels.ForumSettings viewModel = new();

			viewModel.Forum = await this.ForumsManager.Get(this.Context.Site, id); 
			await BuildForumSettingsViewModel(viewModel, false);
						
			viewModel.ForumPermissions = viewModel.Forum.Permissions.ToPermissionsList(this.Context.Site);

			return viewModel;
		}

		private async Task<IEnumerable<SelectListItem>> GetAvailableRoles(List<Permission> existingPermissions)
		{
			IEnumerable<Role> availableRoles = (await this.RoleManager.List(this.Context.Site)).Where
			(
				role => role.Id != this.Context.Site.AdministratorsRole?.Id && (existingPermissions==null || !existingPermissions.Where(item => item.Role.Id == role.Id).Any())
			);

			IEnumerable<string> roleGroups = availableRoles
				.Where(role => role.RoleGroup != null)
				.Select(role => role.RoleGroup.Name)
				.Distinct()
				.OrderBy(name => name);

			Dictionary<string, SelectListGroup> groups = roleGroups.ToDictionary(name => name, name => new SelectListGroup() { Name = name });

			return availableRoles.Select(role => new SelectListItem(role.Name, role.Id.ToString())
			{
				Group = groups.Where(group => role.RoleGroup != null && role.RoleGroup.Name == group.Key).FirstOrDefault().Value
			})
			.OrderBy(selectListItem => selectListItem.Group?.Name);
		}

		private async Task<List<Permission>> RebuildPermissions(PermissionsList permissions)
		{
			if (permissions == null) return null;

			foreach (KeyValuePair<Guid, PermissionsListItem> rolePermissions in permissions)
			{
				foreach (Permission permission in rolePermissions.Value.Permissions)
				{
					permission.Role = await this.RoleManager.Get(rolePermissions.Key);

					rolePermissions.Value.Role = permission.Role;
				}
			}

			return permissions.ToList();
		}
	}
}