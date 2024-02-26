using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Models.Paging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nucleus.Modules.FilesList.Controllers
{
	[Extension("FilesList")]
	public class FilesListViewerController : Controller
	{
		private Context Context { get; }
		private FolderOptions FolderOptions { get; }

		private IPageModuleManager PageModuleManager { get; }
		private IFileSystemManager FileSystemManager { get; }
		private IRoleManager RoleManager { get; }
		private ILogger<FilesListViewerController> Logger { get; }

		public FilesListViewerController(Context Context, IOptions<FolderOptions> folderOptions, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager, IRoleManager roleManager, ILogger<FilesListViewerController> logger)
		{
			this.Context = Context;
			this.FolderOptions = folderOptions.Value;
			this.PageModuleManager = pageModuleManager;
			this.FileSystemManager = fileSystemManager;
			this.RoleManager = roleManager;
			this.Logger = logger;
		}

		[HttpGet]
		public async Task<ActionResult> Index()
		{
			return View("Viewer", await BuildViewModel());
		}

		private async Task<ViewModels.Viewer> BuildViewModel()
		{
			Folder folder;
			PagingSettings settings = new()
			{
				PageSizes = new() { 250, 500 },
				PageSize = 500
			};

			ViewModels.Viewer viewModel = new();			
			viewModel.GetSettings(this.Context.Module);

			if (viewModel.SourceFolderId != Guid.Empty)
			{
				try
				{
					folder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SourceFolderId);

					if (folder != null)
					{
						folder = await this.FileSystemManager.ListFolder(this.Context.Site, folder.Id, HttpContext.User, "");

						viewModel.Files = folder.Files;
					}
				}
				catch (System.IO.FileNotFoundException)
				{
					// this handles the case where the "most recent" folder has been deleted
					folder = null;
				}
			}

			viewModel.LayoutPath = $"ViewerLayouts/{viewModel.Layout}.cshtml";

			if (!System.IO.File.Exists($"{this.FolderOptions.GetExtensionFolder("FilesList", false)}//Views/{viewModel.LayoutPath}"))
			{
				viewModel.LayoutPath = $"ViewerLayouts/Table.cshtml";
			}

			return viewModel;
		}
	}
}