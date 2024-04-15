using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Models.Permissions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Extensions;
using System.IO.Compression;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Extensions.Authorization;
using Nucleus.Abstractions.Models.Extensions;
using Nucleus.Abstractions.Models.Paging;
using Nucleus.Abstractions.Search;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
  public class FileSystemController : Controller
	{
		private const string CURRENT_FOLDER_COOKIE_NAME = "nucleus-current-folder";

		private ILogger<FileSystemController> Logger { get; }
		private Context Context { get; }
		private IFileSystemManager FileSystemManager { get; }
		private IRoleManager RoleManager { get; }
    private IUserManager UserManager { get; }
    private FileSystemProviderFactoryOptions FileSystemOptions { get; }
    private IEnumerable<ISearchProvider> SearchProviders { get; }

    public FileSystemController(ILogger<FileSystemController> Logger, Context Context, IEnumerable<ISearchProvider> searchProviders, IOptions<FileSystemProviderFactoryOptions> fileSystemOptions, IUserManager userManager, IRoleManager roleManager, IFileSystemManager fileSystemManager)
		{
			this.Logger = Logger;
			this.Context = Context;
			this.FileSystemOptions = fileSystemOptions.Value;
      this.UserManager = userManager;
			this.RoleManager = roleManager;
			this.FileSystemManager = fileSystemManager;
      this.SearchProviders = searchProviders;
		}

		/// <summary>
		/// Handle the initial GET request for the files page.
		/// </summary>		
		/// <remarks>
		/// This action checks for a "current folder cookie".
		/// </remarks>
		[HttpGet]
		public async Task<ActionResult> Index(Guid folderId)
		{
			if (folderId == Guid.Empty)
			{
				// Use the current folder cookie.  This cookie is created/updated in BuildViewModel().
				_ = Guid.TryParse(ControllerContext.HttpContext.Request.Cookies["nucleus-current-folder"], out folderId);
			}

			return await Navigate(new(), folderId, Guid.Empty);
		}

    /// <summary>
		/// Display the selected file's folder, with the file highlighted.
		/// </summary>		
		/// <remarks>
		/// This action checks for a "current folder cookie".
		/// </remarks>
		[HttpGet]
    public async Task<ActionResult> SelectFile(Guid fileId)
    {
      File file = await this.FileSystemManager.GetFile(this.Context.Site, fileId);
      return await Navigate(new(), file.Parent.Id, fileId); 
    }

    /// <summary>
    /// Handle subsequent folder navigation.
    /// </summary>
    /// <remarks>
    /// This action DOES NOT check for a "current folder cookie".
    /// </remarks>		
    [HttpPost]
		public async Task<ActionResult> Navigate(ViewModels.Admin.FileSystem viewModel, Guid folderId, Guid fileId)
		{
			Folder folder;

      if (folderId == Guid.Empty)
      {
        folderId = viewModel.Folder.Id;
      }

      if (folderId == Guid.Empty)
			{
				folder = null;
			}
			else
			{
				try
				{
					folder = await this.FileSystemManager.GetFolder(this.Context.Site, folderId);

          // handle provider change/selection in the UI
          if (viewModel.SelectedProviderKey != null && folder?.Provider != viewModel.SelectedProviderKey)
          {
            folder = null;
          }
        }
				catch (System.IO.FileNotFoundException)
				{
					// this handles the case where the "most recent" folder has been deleted
					folder = null;
				}
			}

			viewModel = await BuildViewModel(viewModel, folder, fileId);

			return View("Index", viewModel);
		}

    [HttpPost]
    public async Task<ActionResult> Search(ViewModels.Admin.FileSystemSearchResults viewModel)
    {
      if (!String.IsNullOrEmpty(viewModel.SearchTerm))
      {
        ISearchProvider searchProvider = null;

        searchProvider = this.SearchProviders.FirstOrDefault();
        
        if (searchProvider == null)
        {
          throw new InvalidOperationException("There is no search provider selected.");
        }
                
        // we have to keep re-populating the page sizes, because they aren't the default, and available page sizes aren't sent back in the response
        viewModel.PagingSettings.PageSizes = new() { 250, 500 };
        
        viewModel.Results = await searchProvider.Search(await BuildSearchQuery(viewModel.SearchTerm, viewModel.PagingSettings));

        // add the path to file titles
        foreach (SearchResult searchResult in viewModel.Results.Results)
        {
          if (searchResult.Scope == Nucleus.Abstractions.Models.FileSystem.File.URN)
          {
            try
            {
              File file = await this.FileSystemManager.GetFile(this.Context.Site, searchResult.SourceId.Value);
              searchResult.Title = $"{searchResult.Title} [{file.Parent.Provider}/{file.Parent.Path}]";
            }
            catch (System.IO.FileNotFoundException) 
            { 
              // suppress exception
            }
          }
        }

        viewModel.PagingSettings.TotalCount = Convert.ToInt32(viewModel.Results.Total);
      }

      return View("_SearchResults", viewModel);
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
		public async Task<ActionResult> EditFolderSettings(ViewModels.Admin.FileSystem viewModel)
		{
			return View("FolderSettings", await BuildViewModel(viewModel, true));
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

			return View("FolderSettings", await BuildViewModel(viewModel, false));
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

			return View("FolderSettings", await BuildViewModel(viewModel, false));
		}

		[HttpPost]
		public async Task<ActionResult> SaveFolderSettings(ViewModels.Admin.FileSystem viewModel)
		{
			Folder folder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Folder.Id);
			viewModel.Folder.CopyDatabaseValuesTo(folder);
			folder.Permissions = await ConvertPermissions(viewModel.FolderPermissions);

			await this.FileSystemManager.SaveFolder(this.Context.Site, folder);
			await this.FileSystemManager.SaveFolderPermissions(this.Context.Site, folder);

      return await Navigate(new(), viewModel.Folder.Id, Guid.Empty);
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

			return View("Index", await BuildViewModel(new ViewModels.Admin.FileSystem(), newFolder, Guid.Empty));
		}

		[HttpPost]
		public async Task<ActionResult> ShowDeleteDialog(ViewModels.Admin.FileSystem viewModel)
		{
      IEnumerable<Folder> selectedFolders = viewModel.Folders
        .Where(folder => folder.IsSelected);

			IEnumerable<File> selectedFiles = viewModel.Files
        .Where(file => file.IsSelected);

      if (!selectedFolders.Any() && !selectedFiles.Any())
			{
				return BadRequest(new ProblemDetails()
				{
					Title = "Delete",
					Detail = "Please select one or more files or folders."
				});
			}

			List<string> nonEmptyFolders = new();
			foreach (Folder selectedFolder in selectedFolders)
			{
				Folder folderDetails = await this.FileSystemManager.ListFolder(this.Context.Site, selectedFolder.Id, HttpContext.User, "");
				if (folderDetails != null && (folderDetails.Files.Any() || folderDetails.Folders.Any()))
				{
					nonEmptyFolders.Add($"'{folderDetails.Name}'");
				}
			}

			if (nonEmptyFolders.Any())
			{
				if (nonEmptyFolders.Count > 1)
				{
					string last = nonEmptyFolders.Last();
					nonEmptyFolders.Remove(last);
					nonEmptyFolders.Add($"and {last}");
				}

				return BadRequest(new ProblemDetails()
				{
					Title = "Delete",
					Detail = $"The folder{(nonEmptyFolders.Count > 1 ? "s" : "")} {String.Join(", ", nonEmptyFolders)} contain{(nonEmptyFolders.Count == 1 ? "s" : "")} one or more files or folders.  You must delete the folders/files within {(nonEmptyFolders.Count > 1 ? "each" : "this")} folder before you can delete it."
				});
			}

			return View("Delete", await BuildDeleteViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> Download(ViewModels.Admin.FileSystem viewModel)
		{
      IEnumerable<Folder> selectedFolders = viewModel.Folders
        .Where(folder => folder.IsSelected);

      IEnumerable<File> selectedFiles = viewModel.Files
        .Where(file => file.IsSelected);

      if ( !selectedFolders.Any() && !selectedFiles.Any())
			{
				return NoContent();// (new { Title = "Download", Detail = "Please select one or more files or folders." });
			}

			if (!selectedFolders.Any() && selectedFiles.Count() == 1)
			{
				// one file selected, download as-is
				File downloadFile = await this.FileSystemManager.GetFile(this.Context.Site, selectedFiles.First().Id);

        // Response.Headers.Add("Content-Disposition", "attachment;filename=" + file.Name);
        Response.GetTypedHeaders().ContentDisposition = new("attachment")
        {
          FileName = downloadFile.Name
        };
				return File(await this.FileSystemManager.GetFileContents(this.Context.Site, downloadFile), downloadFile.GetMIMEType(true));
			}

			System.IO.MemoryStream output = new();
			ZipArchive archive = new(output, ZipArchiveMode.Create, true, System.Text.Encoding.UTF8);

			foreach (var item in selectedFolders)
			{
				await AddFolderToZip(archive, item);
			}
		
			foreach (File selectedItem in selectedFiles)
			{
				File file = await this.FileSystemManager.GetFile(this.Context.Site, selectedItem.Id);
				ZipArchiveEntry entry = archive.CreateEntry(file.Name);
				using (System.IO.Stream outputStream = entry.Open())
				{
					using (System.IO.Stream inputStream = await this.FileSystemManager.GetFileContents(this.Context.Site, file))
					{
						await (inputStream).CopyToAsync(outputStream);
					}
				}
			}

			archive.Dispose();
			output.Position = 0;
      //Response.Headers.Add("Content-Disposition", "attachment");
      Response.GetTypedHeaders().ContentDisposition = new("attachment");
      return File(output, "application/zip");
		}

		private async Task AddFolderToZip(ZipArchive archive, Folder folder)
		{
			// the folder object won't be fully populated from model binding, so we have to re-read it 
			folder = await this.FileSystemManager.ListFolder(this.Context.Site, folder.Id, HttpContext.User, "");

			foreach (File file in folder.Files)
			{
				// Zip file paths always use "\" as a delimiter
				ZipArchiveEntry entry = archive.CreateEntry(file.Path.Replace('/', '\\'));
				using (System.IO.Stream stream = entry.Open())
				{
					await(await this.FileSystemManager.GetFileContents(this.Context.Site, file)).CopyToAsync(stream);
				}
			}

			foreach (Folder subFolder in folder.Folders)
			{
				await AddFolderToZip(archive, subFolder);
			}
		}

		[HttpPost]
		public async Task<ActionResult> DeleteSelected(ViewModels.Admin.FileSystemDelete viewModel)
		{
			viewModel.Folder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Folder.Id);

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

			return View("Index", await BuildViewModel(new ViewModels.Admin.FileSystem() { SelectedProviderKey = viewModel.Folder.Provider }, viewModel.Folder, Guid.Empty));
		}

		[HttpPost]
		public async Task<ActionResult> ShowRenameDialog(ViewModels.Admin.FileSystem viewModel)
		{
      FileSystemItem existing;

      existing = viewModel.Folders.Where(folder => folder.IsSelected).FirstOrDefault();
      if (existing != null)
      {
        viewModel.SelectedItem = await this.FileSystemManager.GetFolder(this.Context.Site, existing.Id);
      }
      else
      {
        existing = viewModel.Files.Where(file => file.IsSelected).FirstOrDefault();
        if (existing != null)
        {
          viewModel.SelectedItem = await this.FileSystemManager.GetFile(this.Context.Site, existing.Id);
        }
      }

      if (viewModel.SelectedItem == null)
			{
        //return BadRequest("Please select an item to rename.");
        return BadRequest(new ProblemDetails()
        {
          Title = "Rename",
          Detail = "Please select an item to rename."
        });
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

			viewModel = await BuildViewModel(viewModel, viewModel.Folder, viewModel.SelectedItem.Id);

			return View("Index", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> UploadFile(ViewModels.Admin.FileSystem viewModel, [FromForm] List<IFormFile> mediaFiles)
		{
      File uploadedFile = null;

			viewModel.Folder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Folder.Id);

			foreach (IFormFile file in mediaFiles)
			{
				if (file != null)
				{
					using (System.IO.Stream fileStream = file.OpenReadStream())
					{
						uploadedFile = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.SelectedProviderKey, viewModel.Folder.Path, file.FileName, fileStream, true);
					}
				}
				else
				{
					return BadRequest();
				}
			}

			viewModel = await BuildViewModel(viewModel, viewModel.Folder, uploadedFile?.Id ?? Guid.Empty);

			return View("Index", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> UploadArchive(ViewModels.Admin.FileSystem viewModel, [FromForm] IFormFile archiveFile)
		{
			viewModel.Folder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Folder.Id);
			if (archiveFile != null)
			{
				using (System.IO.Stream fileStream = archiveFile.OpenReadStream())
				{
					ZipArchive archive = new ZipArchive(fileStream);

					foreach (var entry in archive.Entries.Where(entry => true))
					{
						Boolean isValid = false;
						AllowedFileType fileType = this.FileSystemOptions.AllowedFileTypes.Where(allowedtype => allowedtype.FileExtensions.Contains(System.IO.Path.GetExtension(entry.Name), StringComparer.OrdinalIgnoreCase)).FirstOrDefault();
						if (fileType != null)
						{
							if (fileType.Restricted && !this.User.IsSiteAdmin(this.Context.Site))
							{
								Logger.LogWarning("Zip File upload [filename: {filename}] blocked: File type Permission Denied.", entry.Name);
							}

							using (System.IO.Stream stream = entry.Open())
							{
								isValid = fileType.IsValid(stream);
								if (!isValid)
								{
									Logger.LogError("ALERT: File content of file '{filename}' uploaded by {userid} : signature [{sample}] does not match any of the file signatures for file type {filetype}.", entry.Name, this.User.GetUserId(), BitConverter.ToString(Nucleus.Extensions.AllowedFileTypeExtensions.GetSample(stream)).Replace("-", ""), System.IO.Path.GetExtension(entry.Name));
								}
								else
								{
									string folderName =  System.IO.Path.GetDirectoryName(entry.FullName);																		
									await EnsureFolderExists(viewModel.SelectedProviderKey, viewModel.Folder.Path, folderName);
									await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.SelectedProviderKey, System.IO.Path.Join(viewModel.Folder.Path, folderName), entry.Name, fileStream, true);
								}
							}
						}
					}
				}
			}
			else
			{
				return BadRequest();
			}

			viewModel = await BuildViewModel(viewModel, viewModel.Folder, Guid.Empty);

			return View("Index", viewModel);
		}


		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
		public async Task<ActionResult> CopyPermissionsReplaceAll(ViewModels.Admin.FileSystem viewModel)
		{
			if (await this.FileSystemManager.CopyPermissionsToDescendants(this.Context.Site, viewModel.Folder , User, CopyPermissionOperation.Replace))
			{
				return Json(new { Title = "Copy Permissions to Descendants", Message = "Permissions were copied successfully.", Icon = "alert" });
			}
			else
			{
				return BadRequest();
			}
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
		public async Task<ActionResult> CopyPermissionsMerge(ViewModels.Admin.FileSystem viewModel)
		{
			if (await this.FileSystemManager.CopyPermissionsToDescendants(this.Context.Site, viewModel.Folder, User, CopyPermissionOperation.Merge))
			{
				return Json(new { Title = "Copy Permissions to Descendants", Message = "Permissions were copied successfully.", Icon = "alert" });
			}
			else
			{
				return BadRequest();
			}
		}


		/// <summary>
		/// Make sure that the item folder exists by iterating through the item folders and checking/creating them.
		/// </summary>
		/// <param name="targetFolder"></param>
		/// <param name="itemFolder"></param>
		/// <remarks>
		/// Zip file paths always use "\" as a delimiter, so we shouldn't use System.IO.Path.AltDirectorySeparatorChar / System.IO.Path.DirectorySeparatorChar
		/// which can be different depending on platform.
		/// </remarks>
		private async Task EnsureFolderExists(string provider, string targetFolder, string itemFolder)
		{			
			string subFolderName = targetFolder;
			foreach (string ancestorFolder in itemFolder.Split(new char[] { '\\' }))
			{
				try
				{
					subFolderName = System.IO.Path.Join(subFolderName, ancestorFolder);
					Folder folder = await this.FileSystemManager.GetFolder(this.Context.Site, provider, subFolderName);
				}
				catch (System.IO.FileNotFoundException)
				{
					await this.FileSystemManager.CreateFolder(this.Context.Site, provider, System.IO.Path.GetDirectoryName(subFolderName), System.IO.Path.GetFileName(subFolderName));
				}
			}
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

		private async Task<ViewModels.Admin.FileSystem> BuildViewModel(ViewModels.Admin.FileSystem input, Folder folder, Guid fileId)
		{
			ViewModels.Admin.FileSystem viewModel = new()
			{
				SelectedProviderKey = input?.SelectedProviderKey ?? folder?.Provider,
				Providers = this.FileSystemManager.ListProviders(),
        SelectedFileId = fileId
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
        else
        {
          folder = await this.FileSystemManager.GetFolder(this.Context.Site, folder.Id);
        }

				if (folder != null)
				{
          viewModel.Folder = folder;

          PagingSettings settings = input.PagingSettings;

          if (settings == null)
          {
            settings = new() 
            { 
              PageSizes = new() { 250, 500 }, 
              PageSize = 500 
            };
          }
          else
          {
            // we have to keep re-populating the page sizes, because they aren't the default, and available page sizes aren't sent back in the response
            settings.PageSizes = new() { 250, 500 };              
          }

          PagedResult<FileSystemItem> fileSystemItems = await this.FileSystemManager.ListFolder(this.Context.Site, folder.Id, HttpContext.User, "", settings);
          viewModel.PagingSettings = fileSystemItems;

          viewModel.Folders = fileSystemItems.Items
            .Where(folder => folder is Folder)
            .Select(folder => folder as Folder)
            .ToList();

          viewModel.Files = fileSystemItems.Items
            .Where(file => file is File)
            .Select(file => file as File)
            .ToList();

          // get data for breadcrumb navigation
          Folder ancestor = viewModel.Folder;
					while (ancestor != null && ancestor.Id != Guid.Empty)
					{
						viewModel.Ancestors.Add(ancestor);												
						if (ancestor.Parent == null || ancestor.Parent.Id == Guid.Empty || ancestor.Parent.Id == ancestor.Id)
						{
							break;
						}
						ancestor = await this.FileSystemManager.GetFolder(this.Context.Site, ancestor.Parent.Id);
					}
					viewModel.Ancestors.Reverse();

			    viewModel.EnableDelete =
				    fileSystemItems.Items.Where(item => item.Capabilities.CanDelete).Any();

			    viewModel.EnableRename =
				    fileSystemItems.Items.Where(item => item.Capabilities.CanRename).Any();
				}
			}


			// https://stackoverflow.com/questions/16816184/mvc-crazy-property-lose-its-value-does-html-hiddenfor-bug
			// https://stackoverflow.com/questions/594600/possible-bug-in-asp-net-mvc-with-form-values-being-replaced/30698787#30698787
			// https://newbedev.com/possible-bug-in-asp-net-mvc-with-form-values-being-replaced
			ModelState.Clear();

			// Remember the current folder for 5 minutes.  This cookie is checked/read in Index().
			ControllerContext.HttpContext.Response.Cookies.Delete(CURRENT_FOLDER_COOKIE_NAME);
			ControllerContext.HttpContext.Response.Cookies.Append(CURRENT_FOLDER_COOKIE_NAME, viewModel.Folder.Id.ToString(), new CookieOptions()
			{
				Expires = DateTime.Now.AddMinutes(5),
				HttpOnly = true,
				Path = "/",
				Secure = ControllerContext.HttpContext.Request.IsHttps,
				SameSite = SameSiteMode.Strict
			});

			return viewModel;
		}

		private async Task<ViewModels.Admin.FileSystem> BuildViewModel(ViewModels.Admin.FileSystem viewModel, Boolean getPermissions)
		{
			if (!String.IsNullOrEmpty(viewModel.SelectedProviderKey))
			{
				if (getPermissions)
				{
					viewModel.Folder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.Folder.Id);
					////viewModel.Folder.Permissions = await this.FileSystemManager.ListPermissions(viewModel.Folder);
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

		private async Task<ViewModels.Admin.FileSystemDelete> BuildDeleteViewModel(ViewModels.Admin.FileSystem viewModel)
		{
      ViewModels.Admin.FileSystemDelete results = new()
      {
        Folder = viewModel.Folder,
        SelectedFolders = viewModel.Folders
          .Where(folder => folder.IsSelected)
          .ToList(),

        SelectedFiles = viewModel.Files
          .Where(file => file.IsSelected)
          .ToList()
      };
   
			foreach (Folder folder in results.SelectedFolders)
			{
				(await this.FileSystemManager.GetFolder(this.Context.Site, folder.Id)).CopyTo(folder);
			}
			
			foreach (File file in results.SelectedFiles)
			{
				(await this.FileSystemManager.GetFile(this.Context.Site, file.Id)).CopyTo(file);
			}

			return results;
		}

		private async Task<ViewModels.Admin.FileSystemCreateFolder> BuildCreateFolderViewModel(Folder folder)
		{
			ViewModels.Admin.FileSystemCreateFolder viewModel = new()
			{
				Folder = await this.FileSystemManager.GetFolder(this.Context.Site, folder.Id)
			};

			return viewModel;
		}

    private async Task<SearchQuery> BuildSearchQuery(string searchTerm, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
    {
      SearchQuery searchQuery = new()
      {
        Site = this.Context.Site,
        SearchTerm = searchTerm,
        PagingSettings = pagingSettings
      };

      List<Role> roles = new() { this.Context.Site.AllUsersRole };

      if (HttpContext.User.IsSiteAdmin(this.Context.Site))
      {
        roles = null;  // roles=null means don't filter results by role
      }
      else if (HttpContext.User.IsAnonymous())
      {
        roles.Add(this.Context.Site.AnonymousUsersRole);
      }
      else
      {
        roles.AddRange((await this.UserManager.Get(this.Context.Site, HttpContext.User.GetUserId()))?.Roles);
      }

      searchQuery.Roles = roles;

      searchQuery.IncludedScopes = new List<string>() { Abstractions.Models.FileSystem.Folder.URN, Abstractions.Models.FileSystem.File.URN };
      
      return searchQuery;
    }

  }
}
