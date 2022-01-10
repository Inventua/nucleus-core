using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.FileSystemProviders;
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
		private IFileSystemProviderFactory FileSystemProviderFactory { get; }

		/// <summary>
		/// Create an instance.
		/// </summary>
		/// <param name="Context"></param>
		/// <param name="fileSystemProviderFactory"></param>
		public FileUpload(Context Context, IFileSystemProviderFactory fileSystemProviderFactory)
		{
			this.Context = Context;
			this.FileSystemProviderFactory = fileSystemProviderFactory;
		}

		/// <summary>
		/// Invoke (render) the control.
		/// </summary>
		/// <param name="filter"></param>
		/// <param name="actionName"></param>
		/// <param name="controlName"></param>
		/// <returns></returns>
		public Task<IViewComponentResult> InvokeAsync(string filter, string actionName, string controlName)
		{
			return Task.Run(() => Invoke(filter, actionName, controlName));
		}

		private IViewComponentResult Invoke(string filter, string actionName, string controlName)
		{
			ViewModels.FileUpload viewModel = new()
			{
				ControlName = controlName ?? "mediaFile",
				ActionName = actionName ?? "UploadFile",
				ControllerName = (string)this.ViewContext.RouteData.Values["controller"],
				AreaName = (string)this.ViewContext.RouteData.Values["area"],
				ExtensionName = (string)this.ViewContext.RouteData.Values["extension"],
				Filter = filter
			};

			return View("~/Shared/Controls/Views/FileUpload.cshtml", viewModel);
		}
	}
}
