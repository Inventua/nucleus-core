using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;
using Nucleus.Core.FileSystemProviders;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.ViewFeatures.Controls
{
	[ViewComponent(Name = "FileUpload")]
	public class FileUpload : ViewComponent 
	{
		Context Context { get; }
		FileSystemProviderFactory FileSystemProviderFactory { get; }

		public FileUpload(Context Context, FileSystemProviderFactory fileSystemProviderFactory)
		{
			this.Context = Context;
			this.FileSystemProviderFactory = fileSystemProviderFactory;
		}

		public IViewComponentResult Invoke(string filter, string actionName, string controlName)
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
