using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Managers;

namespace Nucleus.ViewFeatures.Controls
{
	/// <summary>
	/// Folder selector control
	/// </summary>
	[ViewComponent(Name = "FolderSelector")]
	public class FolderSelector : ViewComponent 
	{
		Context Context { get; }
		IFileSystemManager FileSystemManager { get; }

		/// <summary>
		/// Create an instance.
		/// </summary>
		/// <param name="Context"></param>
		/// <param name="fileSystemManager"></param>
		public FolderSelector(Context Context, IFileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.FileSystemManager = fileSystemManager;
		}

		/// <summary>
		/// Invoke (render) the control.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="propertyName"></param>
		/// <param name="pattern"></param>
		/// <returns></returns>
		public async Task<IViewComponentResult> InvokeAsync(Folder model, string propertyName, string pattern)
		{
			ViewModels.FolderSelector viewModel = new() 
			{
				AreaName = (string)this.ViewContext.RouteData.Values["area"],
				ControllerName = (string)this.ViewContext.RouteData.Values["controller"],
				ExtensionName = (string)this.ViewContext.RouteData.Values["extension"],
				SelectedFolder = model,
				PropertyName = propertyName ?? "SelectedFolder",
				Providers = this.FileSystemManager.ListProviders()
			};

			if (viewModel.SelectedFolder != null)
			{
				if (viewModel.SelectedFolder.Id != Guid.Empty)
				{
					Folder folder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedFolder.Id);

					// handle provider change
					if (folder.Provider != viewModel.SelectedFolder.Provider)
					{
						folder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedFolder.Provider, "");
					}

					viewModel.SelectedFolder = folder;
				}
			}

			if (viewModel.SelectedFolder == null)
			{
				viewModel.SelectedFolder = new();
			}

			// https://stackoverflow.com/questions/16816184/mvc-crazy-property-lose-its-value-does-html-hiddenfor-bug
			// https://stackoverflow.com/questions/594600/possible-bug-in-asp-net-mvc-with-form-values-being-replaced/30698787#30698787
			// https://newbedev.com/possible-bug-in-asp-net-mvc-with-form-values-being-replaced
			ModelState.Remove($"{propertyName}.Provider");
      ModelState.Remove($"{propertyName}.Id");
      ModelState.Remove($"{propertyName}.Path");

			// if no provider is selected, default the selected provider key to the first available
			if (String.IsNullOrEmpty(viewModel.SelectedFolder.Provider))
			{
				viewModel.SelectedFolder.Provider = viewModel.Providers.FirstOrDefault()?.Key;
			}

			if (viewModel.SelectedFolder.Id == Guid.Empty)
			{
				viewModel.SelectedFolder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedFolder.Provider, viewModel.SelectedFolder.Path);
			}

			// Populate the folders list with "children" of the currently selected folder
			try
			{
				if (viewModel.SelectedFolder.Id != Guid.Empty)
				{					
					viewModel.SelectedFolder = await this.FileSystemManager.ListFolder(this.Context.Site, viewModel.SelectedFolder.Id, HttpContext.User, pattern);
					viewModel.SelectedFolder.SortFolders(folder => folder.Name, false);
				}
			}
			catch (System.IO.FileNotFoundException)
			{ 
			}

			
			// Add the currently selected folder to the folders list (at the top)
			viewModel.SelectedFolder.Folders.Insert(0, viewModel.SelectedFolder);

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
				viewModel.SelectedFolder.Folders.Insert(0, new Folder() 
				{ 
					Id = viewModel.SelectedFolder.Parent.Id,
					Name = "..",
					Parent = viewModel.SelectedFolder.Parent.Parent,
					Path = viewModel.SelectedFolder.Parent.Path,
					Permissions = viewModel.SelectedFolder.Parent.Permissions,
					Provider = viewModel.SelectedFolder.Parent.Provider
				});
			}

			return View("~/Shared/Controls/Views/FolderSelector.cshtml", viewModel);
		}
	}
}
