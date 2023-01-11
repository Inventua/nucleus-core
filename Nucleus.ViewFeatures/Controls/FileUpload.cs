using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.ViewFeatures.Controls
{
	/// <summary>
	/// File upload control.
	/// </summary>
	[ViewComponent(Name = "FileUpload")]
	public class FileUpload : ViewComponent 
	{
		private Context Context { get; }
		private IFileSystemManager FileSystemManager { get; }

		/// <summary>
		/// Create an instance.
		/// </summary>
		/// <param name="Context"></param>
		/// <param name="fileSystemManager"></param>
		public FileUpload(Context Context, IFileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.FileSystemManager = fileSystemManager;
		}

		/// <summary>
		/// Invoke (render) the control.
		/// </summary>
		/// <param name="folder"></param>
		/// <param name="filter"></param>
		/// <param name="actionName"></param>
		/// <param name="controlName"></param>
    /// <param name="text"></param>
    /// <param name="cssClass"></param>
		/// <returns></returns>
		public async Task<IViewComponentResult> InvokeAsync(Folder folder, string filter, string actionName, string controlName, string text, string cssClass)
		{
			ViewModels.FileUpload viewModel = new()
			{				
        ControlName = controlName ?? "mediaFile",
        Text = text ?? "Upload File",
        CssClass = cssClass ?? "btn btn-secondary ms-auto",
        ActionName = actionName ?? "UploadFile",
				ControllerName = (string)this.ViewContext.RouteData.Values["controller"],
				AreaName = (string)this.ViewContext.RouteData.Values["area"],
				ExtensionName = (string)this.ViewContext.RouteData.Values["extension"],
				Filter = filter
			};

      if (folder != null)
			{
				if (folder.Id != Guid.Empty)
				{
					folder = await this.FileSystemManager.GetFolder(this.Context.Site, folder.Id);
				}
			}

			if (folder == null)
			{
				folder = new();
			}

			// if no provider is selected, default the selected provider key to the first available
			if (String.IsNullOrEmpty(folder.Provider))
			{
				folder.Provider = this.FileSystemManager.ListProviders().FirstOrDefault()?.Key;
			}

			if (folder.Id == Guid.Empty)
			{
				folder = await this.FileSystemManager.GetFolder(this.Context.Site, folder.Provider, folder.Path);
			}

			viewModel.Enabled = folder.Capabilities.CanStoreFiles;

			return View("~/Shared/Controls/Views/FileUpload.cshtml", viewModel);
		}		
	}
}
