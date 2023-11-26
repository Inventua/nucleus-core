using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions.Authorization;

namespace Nucleus.ViewFeatures.Controls
{
	/// <summary>
	/// File selector control.
	/// </summary>
	[ViewComponent(Name = "FileSelector")]
	public class FileSelector : ViewComponent 
	{
		private Context Context { get; }
		private IFileSystemManager FileSystemManager { get; }

		/// <summary>
		/// Create an instance.
		/// </summary>
		/// <param name="Context"></param>
		/// <param name="fileSystemManager"></param>
		public FileSelector(Context Context, IFileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.FileSystemManager = fileSystemManager;
		}

		/// <summary>
		/// Invoke (render) the control.
		/// </summary>
		/// <param name="file"></param>
		/// <param name="pattern"></param>
		/// <param name="propertyName"></param>
		/// <param name="showSelectAnother"></param>
		/// <param name="selectAnotherActionName"></param>
		/// <param name="applicationAbsoluteUrl"></param>
		/// <param name="noFilesMessage"></param>
		/// <param name="showImagePreview"></param>
		/// <returns></returns>
		public async Task<IViewComponentResult> InvokeAsync(File file, string pattern, string propertyName, string selectAnotherActionName, Boolean showSelectAnother = true, Boolean applicationAbsoluteUrl = true, string noFilesMessage = "(no files)", Boolean showImagePreview = false)
		{
			ViewModels.FileSelector viewModel = new() 
			{
				AreaName = (string)this.ViewContext.RouteData.Values["area"],
				ControllerName = (string)this.ViewContext.RouteData.Values["controller"],
				ExtensionName = (string)this.ViewContext.RouteData.Values["extension"],
				SelectedFile = file,
				Providers = this.FileSystemManager.ListProviders(),
				PropertyName = propertyName ?? "SelectedFile",
				ShowSelectAnother = showSelectAnother,
				SelectAnotherActionName = selectAnotherActionName ?? "SelectAnother",
				NoFilesMessage = noFilesMessage ?? "(no files)",
				ShowImagePreview = showImagePreview
			};

			if (viewModel.SelectedFile == null)
			{
				viewModel.SelectedFile = new();
			}
			else
			{
				try
				{
					if (viewModel.SelectedFile.Id != Guid.Empty)
					{
						viewModel.SelectedFile = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.SelectedFile.Id);
					}
					else
					{
						// if SelectedFile is not null but doesn't have an ID, that means the user has selected another folder and does not have a file selected yet
						// We re-populate the .SelectedFolder property below.
					}
				}
				catch (System.IO.FileNotFoundException)
				{
					viewModel.SelectedFile = new();
				}
			}

			// https://stackoverflow.com/questions/16816184/mvc-crazy-property-lose-its-value-does-html-hiddenfor-bug
			// https://stackoverflow.com/questions/594600/possible-bug-in-asp-net-mvc-with-form-values-being-replaced/30698787#30698787
			// https://newbedev.com/possible-bug-in-asp-net-mvc-with-form-values-being-replaced
			ModelState.Remove(viewModel.PropertyName + ".Id"); 
			ModelState.Remove(viewModel.PropertyName + ".Provider");
			ModelState.Remove(viewModel.PropertyName + ".Parent.Id");
			ModelState.Remove(viewModel.PropertyName + ".Path");

			// if no provider is selected, default the selected provider key to the first available
			if (String.IsNullOrEmpty(viewModel.SelectedFile.Provider))
			{
				viewModel.SelectedFile.Provider = viewModel.Providers.FirstOrDefault()?.Key;
			}

			// Populate the folders list with "children" of the currently selected folder
			if ( viewModel.SelectedFile?.Parent != null && viewModel.SelectedFile.Parent.Id != Guid.Empty)
			{
				viewModel.SelectedFolder = await this.FileSystemManager.ListFolder(this.Context.Site, viewModel.SelectedFile.Parent.Id, HttpContext.User, pattern);
			}
			else
			{
        // handle default (top) folder 
        Folder selectedFolder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedFile.Provider, viewModel.SelectedFile.Parent?.Path ?? "");
          
        if (HttpContext.User.HasBrowsePermission(this.Context.Site, selectedFolder))
        {
          viewModel.SelectedFolder = await this.FileSystemManager.ListFolder(this.Context.Site, selectedFolder.Id, HttpContext.User, pattern);
        }
			}

			// copy the selected folder so that changes made below don't get reflected in cache
			viewModel.SelectedFolder = viewModel.SelectedFolder.Copy<Folder>();

			viewModel.SelectedFolder.SortFolders(folder => folder.Name, false);
			viewModel.SelectedFolder.SortFiles(file => file.Name, false);

			// Add the currently selected folder to the folders list (at the top)
			viewModel.SelectedFolder.Folders.Insert(0, viewModel.SelectedFolder);
			//viewModel.SelectedFolder.Folders.Insert(0, new Folder()
			//{
			//	Id = viewModel.SelectedFolder.Id,
			//	Name = $"[{viewModel.SelectedFolder.Name}]"
			//});

			// add a separator after the currently selected folder, only if there are any child folders
			if (viewModel.SelectedFolder.Folders.Count > 1)
			{
				viewModel.SelectedFolder.Folders.Insert(1, new Folder()
				{
					Id = Guid.Empty,
					Name = "-"
				});
			}

			// Add a "up one level" folder to the folders list, unless we are already at the top level
			if (!String.IsNullOrEmpty(viewModel.SelectedFolder.Path))
			{
        Folder parentFolder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedFolder.Parent.Id);

        if (HttpContext.User.HasBrowsePermission(this.Context.Site, parentFolder))
        {
          viewModel.SelectedFolder.Folders.Insert(0, new Folder()
          {
            Id = parentFolder.Id,
            Name = "..",
            Parent = parentFolder.Parent,
            Path = parentFolder.Path,
            Permissions = parentFolder.Permissions,
            Provider = parentFolder.Provider
          });
        }
			}

			if (!String.IsNullOrEmpty(viewModel.SelectedFile.Path))
			{
				// fully populate the SelectedFile property from the file system
				viewModel.SelectedFile = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.SelectedFile.Id);

				// if the user has selected a different provider or folder, set the currenly selected file path to empty
				if (viewModel.SelectedFile.Parent.Provider != viewModel.SelectedFolder.Provider || viewModel.SelectedFile.Parent.Path != viewModel.SelectedFolder.Path)
				{
					viewModel.SelectedFile.Id = Guid.Empty;
					viewModel.SelectedFile.Path = null;
				}
			}

			return View("~/Shared/Controls/Views/FileSelector.cshtml", viewModel);
		}
	}
}
