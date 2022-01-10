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
	[ViewComponent(Name = "FolderSelector")]
	public class FolderSelector : ViewComponent 
	{
		Context Context { get; }
		FileSystemManager FileSystemManager { get; }

		public FolderSelector(Context Context, FileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.FileSystemManager = fileSystemManager;
		}

		public IViewComponentResult Invoke(Folder model, string propertyName, string pattern)
		{
			ViewModels.FolderSelector viewModel = new() 
			{
				AreaName = (string)this.ViewContext.RouteData.Values["area"],
				ControllerName = (string)this.ViewContext.RouteData.Values["controller"],
				ExtensionName = (string)this.ViewContext.RouteData.Values["extension"],
				SelectedFolder = model,
				PropertyName = propertyName ?? "SelectedFolder",
				Providers = new(this.FileSystemManager.ListProviders())
			};

			if (viewModel.SelectedFolder != null)
			{
				if (viewModel.SelectedFolder.Id != Guid.Empty)
				{
					viewModel.SelectedFolder = this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SelectedFolder.Id);
				}
			}

			if (viewModel.SelectedFolder == null)
			{
				viewModel.SelectedFolder = new();
			}

			// https://stackoverflow.com/questions/16816184/mvc-crazy-property-lose-its-value-does-html-hiddenfor-bug
			// https://stackoverflow.com/questions/594600/possible-bug-in-asp-net-mvc-with-form-values-being-replaced/30698787#30698787
			// https://newbedev.com/possible-bug-in-asp-net-mvc-with-form-values-being-replaced
			ModelState.Remove("SelectedFolder.Provider");
			ModelState.Remove("SelectedFolder.Path");

			// if no provider is selected, default the selected provider key to the first available
			if (String.IsNullOrEmpty(viewModel.SelectedFolder.Provider))
			{
				viewModel.SelectedFolder.Provider = viewModel.Providers.Keys.FirstOrDefault();
			}

			// Populate the folders list with "children" of the currently selected folder
			//FileSystemProvider provider = this.FileSystemProviderFactory.Get(viewModel.SelectedFile.Provider);
			viewModel.SelectedFolder = this.FileSystemManager.ListFolder(this.Context.Site, viewModel.SelectedFolder.Provider, viewModel.SelectedFolder.Path, pattern);
		
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

			return View("~/Shared/Controls/Views/FolderSelector.cshtml", viewModel);
		}
	}
}
