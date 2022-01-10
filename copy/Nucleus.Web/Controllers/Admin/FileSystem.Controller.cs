using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Core;
using Nucleus.Core.FileSystemProviders;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Models.Permissions;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
	public class FileSystemController : Controller
	{
		private ILogger<FileSystemController> Logger { get; }
		private Context Context { get; }
		private FileSystemManager FileSystemManager { get; }
		private RoleManager RoleManager { get; }

		public FileSystemController(ILogger<FileSystemController> Logger, Context Context, RoleManager roleManager, FileSystemManager fileSystemManager)
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
		public ActionResult Index(ViewModels.Admin.FileSystem viewModel, string path)
		{
			viewModel = BuildViewModel(viewModel, path);
			return View("Index", viewModel);
		}

		/// <summary>
		/// Display the Create Folder dialog
		/// </summary>
		/// <param name="viewModel"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		[HttpGet]
		public ActionResult ShowCreateFolderDialog(ViewModels.Admin.FileSystem viewModel)
		{
			viewModel = BuildViewModel(viewModel, viewModel.Folder.Path);

			return View("CreateFolder", viewModel);
		}

		/// <summary>
		/// Show the folder permissions UI
		/// </summary>
		/// <param name="viewModel"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult EditFolderPermissions(ViewModels.Admin.FileSystem viewModel)
		{			
			return View("FolderPermissions", BuildViewModel(viewModel, true));
		}

		[HttpPost]
		public ActionResult AddFolderPermissionRole(ViewModels.Admin.FileSystem viewModel)
		{
			viewModel.Folder.Path = viewModel.Folder.Path ?? "";
			if (viewModel.SelectedFolderRoleId != Guid.Empty)
			{
				viewModel.Folder.Permissions = ConvertPermissions(viewModel.FolderPermissions);
				this.FileSystemManager.CreatePermissions(viewModel.Folder, this.RoleManager.Get(viewModel.SelectedFolderRoleId));
			}

			return View("FolderPermissions", BuildViewModel(viewModel, false));			
		}

		[HttpPost]
		public ActionResult DeleteFolderPermissionRole(ViewModels.Admin.FileSystem viewModel, Guid id)
		{
viewModel.Folder.Permissions = ConvertPermissions(viewModel.FolderPermissions);

			foreach (Permission permission in viewModel.Folder.Permissions.ToList())
			{
				if (permission.Role.Id == id)
				{
					viewModel.Folder.Permissions.Remove(permission);
				}
			}

			viewModel.FolderPermissions = viewModel.Folder.Permissions.ToPermissionsList();

			return View("FolderPermissions", BuildViewModel(viewModel, false));
		}

		[HttpPost]
		public ActionResult SaveFolderPermissions(ViewModels.Admin.FileSystem viewModel)
		{
			viewModel.Folder.Path = viewModel.Folder.Path ?? "";
			viewModel.Folder.Permissions = ConvertPermissions(viewModel.FolderPermissions);

			this.FileSystemManager.SaveFolderPermissions(this.Context.Site, viewModel.Folder);

			return Ok();
		}


		/// <summary>
		/// Create the specified folder
		/// </summary>
		/// <param name="viewModel"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult CreateFolder(ViewModels.Admin.FileSystem viewModel)
		{
			this.FileSystemManager.CreateFolder(this.Context.Site, viewModel.SelectedProviderKey, viewModel.Folder.Path, viewModel.NewFolder);

			viewModel = BuildViewModel(viewModel, viewModel.Folder.Path);

			return View("Index", viewModel);
		}

		[HttpPost]
		public ActionResult ShowDeleteDialog(ViewModels.Admin.FileSystem viewModel)
		{			
			// todo: display selected items 
			return View("Delete", viewModel);
		}

		[HttpPost]
		public ActionResult DeleteSelected(ViewModels.Admin.FileSystem viewModel)
		{
			//FileSystemProvider provider = this.FileSystemProviderFactory.Get(viewModel.SelectedProviderKey);

			foreach (var item in viewModel.Folder.Folders.Where(folder => folder.IsSelected))
			{
				this.FileSystemManager.DeleteFolder(this.Context.Site, item, false);
			}

			foreach (var item in viewModel.Folder.Files.Where(folder => folder.IsSelected))
			{
				this.FileSystemManager.DeleteFile(this.Context.Site, item);
			}

			viewModel = BuildViewModel(viewModel, viewModel.Folder.Path);

			return View("Index", viewModel);
		}

		[HttpPost]
		public ActionResult ShowRenameDialog(ViewModels.Admin.FileSystem viewModel)
		{
			//FileSystemProvider provider = this.FileSystemProviderFactory.Get(viewModel.SelectedProviderKey);
			FileSystemItem existing;

			existing = viewModel.Folder.Folders.Where(folder => folder.IsSelected).FirstOrDefault();
			if (existing == null)
			{
				existing = viewModel.Folder.Files.Where(file => file.IsSelected).FirstOrDefault();
			}

			viewModel.SelectedItem = existing;
			//viewModel.SelectedItem = this.FileSystemManager.GetFileSystemItem(this.Context.Site, viewModel.SelectedProviderKey, existing.Path);

			return View("Rename", viewModel);
		}

		[HttpPost]
		public ActionResult Rename(ViewModels.Admin.FileSystem viewModel)
		{
			FileSystemItem existing;

			try
			{
				existing = this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedProviderKey, viewModel.SelectedItem.Path);
			}
			catch (System.IO.FileNotFoundException)
			{
				existing = this.FileSystemManager.GetFile(this.Context.Site, viewModel.SelectedProviderKey, viewModel.SelectedItem.Path);
			}
			
			if (existing == null)
			{
				return NotFound();
			}

			if (existing is File)
			{
				this.FileSystemManager.RenameFile(this.Context.Site, existing as File, viewModel.SelectedItem.Name);
			}
			else if (existing is Folder)
			{
				this.FileSystemManager.RenameFolder(this.Context.Site, viewModel.SelectedProviderKey, existing.Path, viewModel.SelectedItem.Name);
			}

			viewModel = BuildViewModel(viewModel, existing.Parent.Path);

			return View("Index", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> UploadFile(ViewModels.Admin.FileSystem viewModel, [FromForm] IFormFile mediaFile)
		{
			//FileSystemProvider provider = this.FileSystemProviderFactory.Get(viewModel.SelectedProviderKey);

			if (mediaFile != null)
			{				
				using (System.IO.Stream fileStream = mediaFile.OpenReadStream())
				{
					await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.SelectedProviderKey, viewModel.Folder.Path, mediaFile.FileName, fileStream, false);
				}				
			}
			else
			{
				return BadRequest();
			}

			viewModel = BuildViewModel(viewModel, viewModel.Folder.Path);

			return View("Index", viewModel);
		}

		private List<Permission> ConvertPermissions(PermissionsList permissions)
		{
			if (permissions == null) return null;

			RebuildPermissions(permissions);

			return permissions.ToList();
		}

		private void RebuildPermissions(PermissionsList permissions)
		{
			if (permissions == null) return;

			foreach (KeyValuePair<Guid, PermissionsListItem> rolePermissions in permissions)
			{
				foreach (Permission permission in rolePermissions.Value.Permissions)
				{
					permission.Role = this.RoleManager.Get(rolePermissions.Key);

					rolePermissions.Value.Role = permission.Role;
				}
			}
		}

		private ViewModels.Admin.FileSystem BuildViewModel(ViewModels.Admin.FileSystem input, string path)
		{
			ViewModels.Admin.FileSystem viewModel = new()
			{
				SelectedProviderKey = input.SelectedProviderKey,
				Providers = this.FileSystemManager.ListProviders()
			};

			if (String.IsNullOrEmpty(viewModel.SelectedProviderKey))
			{
				viewModel.SelectedProviderKey = viewModel.Providers.Keys.FirstOrDefault();
			}

			if (!String.IsNullOrEmpty(viewModel.SelectedProviderKey))
			{
				//FileSystemProvider provider = this.FileSystemProviderFactory.Get(viewModel.SelectedProviderKey);
				viewModel.Folder = this.FileSystemManager.ListFolder(this.Context.Site, viewModel.SelectedProviderKey, path);
			}

			// https://stackoverflow.com/questions/16816184/mvc-crazy-property-lose-its-value-does-html-hiddenfor-bug
			// https://stackoverflow.com/questions/594600/possible-bug-in-asp-net-mvc-with-form-values-being-replaced/30698787#30698787
			// https://newbedev.com/possible-bug-in-asp-net-mvc-with-form-values-being-replaced
			ModelState.Clear();

			return viewModel;
		}

		private ViewModels.Admin.FileSystem BuildViewModel(ViewModels.Admin.FileSystem viewModel, Boolean getPermissions)
		{
			if (!String.IsNullOrEmpty(viewModel.SelectedProviderKey))
			{
				//FileSystemProvider provider = this.FileSystemProviderFactory.Get(viewModel.SelectedProviderKey);
								
				if (getPermissions)
				{
				  viewModel.Folder = this.FileSystemManager.ListFolder(this.Context.Site, viewModel.SelectedProviderKey, viewModel.Folder.Path);
					Folder existing = this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Folder.Provider, viewModel.Folder.Path);
					if (existing != null)
					{
						viewModel.Folder.Id = existing.Id;
					}
					viewModel.Folder.Permissions = this.FileSystemManager.ListPermissions(viewModel.Folder);
					
				}
			}
						
			viewModel.AvailableFolderRoles = this.RoleManager.List(this.Context.Site).Where
			(
				role => role.Id != this.Context.Site.AdministratorsRole?.Id && !viewModel.Folder.Permissions.Where(item => item.Role.Id == role.Id).Any()
			);

			viewModel.FolderPermissionTypes = this.FileSystemManager.ListFolderPermissionTypes();
			viewModel.FolderPermissions = viewModel.Folder.Permissions.ToPermissionsList();

			return viewModel;
		}
	}
}
