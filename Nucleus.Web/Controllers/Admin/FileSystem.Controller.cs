using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Models.Permissions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Extensions;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
	public class FileSystemController : Controller
	{
		private ILogger<FileSystemController> Logger { get; }
		private Context Context { get; }
		private IFileSystemManager FileSystemManager { get; }
		private IRoleManager RoleManager { get; }

		public FileSystemController(ILogger<FileSystemController> Logger, Context Context, IRoleManager roleManager, IFileSystemManager fileSystemManager)
		{
			this.Logger = Logger;
			this.Context = Context;
			this.RoleManager = roleManager;
			this.FileSystemManager = fileSystemManager;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Index(ViewModels.Admin.FileSystem viewModel, Guid folderId)
		{
			Folder folder;
			if (folderId == Guid.Empty)
			{
				folder = null;
			}
			else
			{
				folder = await this.FileSystemManager.GetFolder(this.Context.Site, folderId);
			}

			viewModel = await BuildViewModel(viewModel, folder);
			return View("Index", viewModel);
		}

		/// <summary>
		/// Display the Create Folder dialog
		/// </summary>
		/// <param name="viewModel"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> ShowCreateFolderDialog(Guid folderId)
		{
			return View("CreateFolder", await BuildCreateFolderViewModel(new Folder() { Id = folderId }));
		}

		/// <summary>
		/// Show the folder permissions UI
		/// </summary>
		/// <param name="viewModel"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		[HttpPost]
		public async Task<ActionResult> EditFolderPermissions(ViewModels.Admin.FileSystem viewModel)
		{			
			return View("FolderPermissions", await BuildViewModel(viewModel, true));
		}

		[HttpPost]
		public async Task<ActionResult> AddFolderPermissionRole(ViewModels.Admin.FileSystem viewModel)
		{
			viewModel.Folder.Path = viewModel.Folder.Path ?? "";
			if (viewModel.SelectedFolderRoleId != Guid.Empty)
			{
				viewModel.Folder.Permissions = await ConvertPermissions(viewModel.FolderPermissions);
				await this.FileSystemManager.CreatePermissions(this.Context.Site, viewModel.Folder, await this.RoleManager.Get(viewModel.SelectedFolderRoleId));
			}

			return View("FolderPermissions", await BuildViewModel(viewModel, false));			
		}

		[HttpPost]
		public async Task<ActionResult> DeleteFolderPermissionRole(ViewModels.Admin.FileSystem viewModel, Guid id)
		{
			viewModel.Folder.Permissions = await ConvertPermissions(viewModel.FolderPermissions);

			foreach (Permission permission in viewModel.Folder.Permissions.ToList())
			{
				if (permission.Role.Id == id)
				{
					viewModel.Folder.Permissions.Remove(permission);
				}
			}

			viewModel.FolderPermissions = viewModel.Folder.Permissions.ToPermissionsList(this.Context.Site);

			return View("FolderPermissions", await BuildViewModel(viewModel, false));
		}

		[HttpPost]
		public async Task<ActionResult> SaveFolderPermissions(ViewModels.Admin.FileSystem viewModel)
		{
			viewModel.Folder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Folder.Id);
			viewModel.Folder.Permissions = await ConvertPermissions(viewModel.FolderPermissions);

			await this.FileSystemManager.SaveFolderPermissions(this.Context.Site, viewModel.Folder);

			return Ok();
		}


		/// <summary>
		/// Create the specified folder
		/// </summary>
		/// <param name="viewModel"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		[HttpPost]
		public async Task<ActionResult> CreateFolder(ViewModels.Admin.FileSystemCreateFolder viewModel)
		{
			Folder parentFolder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Folder.Id);
			Folder newFolder = await this.FileSystemManager.CreateFolder(this.Context.Site, parentFolder.Provider, parentFolder.Path, viewModel.NewFolder);

			return View("Index", await BuildViewModel(new ViewModels.Admin.FileSystem(), newFolder));
		}

		[HttpPost]
		public ActionResult ShowDeleteDialog(ViewModels.Admin.FileSystem viewModel)
		{			
			return View("Delete", BuildDeleteViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> DeleteSelected(ViewModels.Admin.FileSystemDelete viewModel)
		{
			if (viewModel.SelectedFolders != null)
			{
				foreach (var item in viewModel.SelectedFolders)
				{
					await this.FileSystemManager.DeleteFolder(this.Context.Site, await this.FileSystemManager.GetFolder(this.Context.Site, item.Id), false);
				}
			}

			if (viewModel.SelectedFiles != null)
			{
				foreach (var item in viewModel.SelectedFiles)
				{
					await this.FileSystemManager.DeleteFile(this.Context.Site, await this.FileSystemManager.GetFile(this.Context.Site, item.Id));
				}
			}

			return View("Index", await BuildViewModel(new ViewModels.Admin.FileSystem(), viewModel.Folder));
		}

		[HttpPost]
		public async Task<ActionResult> ShowRenameDialog(ViewModels.Admin.FileSystem viewModel)
		{
			FileSystemItem existing;

			existing = viewModel.Folder.Folders.Where(folder => folder.IsSelected).FirstOrDefault();
			if (existing != null)
			{
				viewModel.SelectedItem = await this.FileSystemManager.GetFolder(this.Context.Site, existing.Id);
			}
			else
			{
				existing = viewModel.Folder.Files.Where(file => file.IsSelected).FirstOrDefault();
				if (existing != null)
				{
					viewModel.SelectedItem = await this.FileSystemManager.GetFile(this.Context.Site, existing.Id);
				}
			}

			if (viewModel.SelectedItem == null)
			{
				return BadRequest("Please select an item to rename.");
			}

			return View("Rename", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> Rename(ViewModels.Admin.FileSystem viewModel)
		{
			FileSystemItem existing;

			try
			{
				existing = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedItem.Id);
			}
			catch (System.IO.FileNotFoundException)
			{
				try
				{
					existing = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.SelectedItem.Id);
				}
				catch (System.IO.FileNotFoundException)
				{
					existing = null;
				}
			}
			
			if (existing == null)
			{
				return NotFound();
			}

			if (existing is File)
			{
				await this.FileSystemManager.RenameFile(this.Context.Site, existing as File, viewModel.SelectedItem.Name);
			}
			else if (existing is Folder)
			{
				await this.FileSystemManager.RenameFolder(this.Context.Site, existing as Folder, viewModel.SelectedItem.Name);
			}

			viewModel = await BuildViewModel(viewModel, viewModel.Folder);

			return View("Index", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> UploadFile(ViewModels.Admin.FileSystem viewModel, [FromForm] IFormFile mediaFile)
		{
			viewModel.Folder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Folder.Id);
			if (mediaFile != null)
			{				
				using (System.IO.Stream fileStream = mediaFile.OpenReadStream())
				{
					await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.SelectedProviderKey, viewModel.Folder.Path, mediaFile.FileName, fileStream, true);
				}				
			}
			else
			{
				return BadRequest();
			}

			viewModel = await BuildViewModel(viewModel, viewModel.Folder);

			return View("Index", viewModel);
		}

		private async Task<List<Permission>> ConvertPermissions(PermissionsList permissions)
		{
			if (permissions == null) return null;

			await RebuildPermissions(permissions);

			return permissions.ToList();
		}

		private async Task RebuildPermissions(PermissionsList permissions)
		{
			if (permissions == null) return;

			foreach (KeyValuePair<Guid, PermissionsListItem> rolePermissions in permissions)
			{
				foreach (Permission permission in rolePermissions.Value.Permissions)
				{
					permission.Role = await this.RoleManager.Get(rolePermissions.Key);

					rolePermissions.Value.Role = permission.Role;
				}
			}
		}

		private async Task<ViewModels.Admin.FileSystem> BuildViewModel(ViewModels.Admin.FileSystem input, Folder folder)
		{
			ViewModels.Admin.FileSystem viewModel = new()
			{
				SelectedProviderKey = input?.SelectedProviderKey ?? folder?.Provider,
				Providers = this.FileSystemManager.ListProviders()
			};

			if (String.IsNullOrEmpty(viewModel.SelectedProviderKey))
			{
				viewModel.SelectedProviderKey = viewModel.Providers.FirstOrDefault()?.Key;
			}

			if (!String.IsNullOrEmpty(viewModel.SelectedProviderKey))
			{
				if (folder == null)
				{
					folder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedProviderKey, "");
				}
				
				if (folder != null)
				{
					viewModel.Folder = await this.FileSystemManager.ListFolder(this.Context.Site, folder.Id, "");
				}
			}

			viewModel.EnableDelete = 
				viewModel.Folder.Folders.Where(folder => folder.Capabilities.CanDelete).Any() ||
				viewModel.Folder.Files.Where(file => file.Capabilities.CanDelete).Any();

			viewModel.EnableRename =
				viewModel.Folder.Folders.Where(folder => folder.Capabilities.CanRename).Any() ||
				viewModel.Folder.Files.Where(file => file.Capabilities.CanRename).Any();

			// https://stackoverflow.com/questions/16816184/mvc-crazy-property-lose-its-value-does-html-hiddenfor-bug
			// https://stackoverflow.com/questions/594600/possible-bug-in-asp-net-mvc-with-form-values-being-replaced/30698787#30698787
			// https://newbedev.com/possible-bug-in-asp-net-mvc-with-form-values-being-replaced
			ModelState.Clear();

			return viewModel;
		}

		private async Task<ViewModels.Admin.FileSystem> BuildViewModel(ViewModels.Admin.FileSystem viewModel, Boolean getPermissions)
		{
			if (!String.IsNullOrEmpty(viewModel.SelectedProviderKey))
			{
				if (getPermissions)
				{
				  viewModel.Folder = await this.FileSystemManager.ListFolder(this.Context.Site, viewModel.Folder.Id, "");
					viewModel.Folder.Permissions = await this.FileSystemManager.ListPermissions(viewModel.Folder);					
				}
			}

			viewModel.AvailableFolderRoles = await GetAvailableRoles(viewModel.Folder?.Permissions);

			viewModel.FolderPermissionTypes = await this.FileSystemManager.ListFolderPermissionTypes();
			viewModel.FolderPermissions = viewModel.Folder.Permissions.ToPermissionsList(this.Context.Site);

			return viewModel;
		}

		private async Task<IEnumerable<SelectListItem>> GetAvailableRoles(List<Permission> existingPermissions)
		{
			IEnumerable<Role> availableRoles = (await this.RoleManager.List(this.Context.Site)).Where
			(
				role => role.Id != this.Context.Site.AdministratorsRole?.Id && (existingPermissions == null || !existingPermissions.Where(item => item.Role.Id == role.Id).Any())
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

		static ViewModels.Admin.FileSystemDelete BuildDeleteViewModel(ViewModels.Admin.FileSystem viewModel)
		{
			return new()
			{
				Folder = viewModel.Folder,
				SelectedFolders = viewModel.Folder?.Folders.Where(folder => folder.IsSelected).ToList(),
				SelectedFiles = viewModel.Folder?.Files.Where(file => file.IsSelected).ToList()
			};
		}

		private async Task<ViewModels.Admin.FileSystemCreateFolder> BuildCreateFolderViewModel(Folder folder)
		{
			ViewModels.Admin.FileSystemCreateFolder viewModel = new()
			{
				Folder = await this.FileSystemManager.GetFolder(this.Context.Site, folder.Id)
			};

			return viewModel;
		}
	}
}
