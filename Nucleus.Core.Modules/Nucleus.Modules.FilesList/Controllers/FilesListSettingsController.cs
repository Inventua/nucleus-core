using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Nucleus.Modules.FilesList.Controllers
{
	[Extension("FilesList")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
	public class FilesListSettingsController : Controller
	{
		private Context Context { get; }
		private FolderOptions FolderOptions { get; }
		private IPageModuleManager PageModuleManager { get; }
		private IFileSystemManager FileSystemManager { get; }

		public FilesListSettingsController(Context Context, IOptions<FolderOptions> folderOptions, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.FolderOptions = folderOptions.Value;
			this.PageModuleManager = pageModuleManager;
			this.FileSystemManager = fileSystemManager;
		}

		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", await BuildSettingsViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
		{
			if (viewModel.SelectedFolder != null)
			{
				viewModel.SourceFolderId = viewModel.SelectedFolder.Id;
			}

			viewModel.SetSettings(this.Context.Module);

			await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

			return Ok();
		}

		private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.GetSettings(this.Context.Module);

			if (viewModel.SelectedFolder == null)
			{
				try
				{
					viewModel.SelectedFolder = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.SourceFolderId);
					viewModel.SourceFolderId= viewModel.SelectedFolder.Id;
				}
				catch (Exception)
				{
					viewModel.SelectedFolder = await this.FileSystemManager.GetFolder(this.Context.Site, this.FileSystemManager.ListProviders().First()?.Key, "");
				}
			}

			viewModel.Layouts = new();
			foreach (string file in System.IO.Directory.EnumerateFiles($"{this.FolderOptions.GetExtensionFolder("FilesList", false)}/Views/ViewerLayouts/", "*.cshtml").OrderBy(layout => layout))
			{
				viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file));
			}

			return viewModel;
		}
	}
}