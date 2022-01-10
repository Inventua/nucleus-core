using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;
using Nucleus.Core;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.ViewFeatures.Controls
{
	[ViewComponent(Name = "FileSelector")]
	public class FileSelector : ViewComponent 
	{
		Context Context { get; }
		FileSystemManager FileSystemManager { get; }

		public FileSelector(Context Context, FileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.FileSystemManager = fileSystemManager;
		}

		public IViewComponentResult Invoke(File model, string pattern, string propertyName, string selectAnotherActionName)
		{
			ViewModels.FileSelector viewModel = new() 
			{
				AreaName = (string)this.ViewContext.RouteData.Values["area"],
				ControllerName = (string)this.ViewContext.RouteData.Values["controller"],
				ExtensionName = (string)this.ViewContext.RouteData.Values["extension"],
				SelectedFile = model,
				Providers = new(this.FileSystemManager.ListProviders()),
				PropertyName = propertyName ?? "SelectedFile",
				SelectAnotherActionName = selectAnotherActionName ?? "SelectAnother"
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
						viewModel.SelectedFile = this.FileSystemManager.GetFile(this.Context.Site, viewModel.SelectedFile.Id);
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
				viewModel.SelectedFile.Provider = viewModel.Providers.Keys.FirstOrDefault();
			}

			// Populate the folders list with "children" of the currently selected folder
			//FileSystemProvider provider = this.FileSystemProviderFactory.Get(viewModel.SelectedFile.Provider);
			//viewModel.SelectedFolder = this.FileSystemManager.ListFolder(this.Context.Site, viewModel.SelectedFile.Provider, viewModel.SelectedFile.Parent?.Path, pattern);
			if (viewModel.SelectedFile?.Parent != null && viewModel.SelectedFile.Parent.Id != Guid.Empty)
			{
				viewModel.SelectedFolder = this.FileSystemManager.ListFolder(this.Context.Site, viewModel.SelectedFile.Parent.Id, pattern);
			}
			else
			{
				viewModel.SelectedFolder = this.FileSystemManager.ListFolder(this.Context.Site, viewModel.SelectedFile.Provider, viewModel.SelectedFile.Parent?.Path, pattern);
			}
				
			// Add the currently selected folder to the folders list (at the top)
			viewModel.SelectedFolder.Folders.Insert(0, viewModel.SelectedFolder);

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

			if (!String.IsNullOrEmpty(viewModel.SelectedFile.Path))
			{
				// fully populate the SelectedFile property from the file system
				viewModel.SelectedFile = this.FileSystemManager.GetFile(this.Context.Site, viewModel.SelectedFile.Provider, viewModel.SelectedFile.Path);

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
