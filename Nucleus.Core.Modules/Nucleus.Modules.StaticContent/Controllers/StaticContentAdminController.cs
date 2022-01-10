using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Modules.StaticContent.Controllers
{
	[Extension("StaticContent")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
	public class StaticContentAdminController : Controller
	{
		internal const string MODULESETTING_SOURCE_FOLDER_ID = "staticcontent:sourcefolder-id";
		internal const string MODULESETTING_DEFAULT_FILE_ID = "staticcontent:defaultfile-id";

		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private IFileSystemManager FileSystemManager { get; }

		public StaticContentAdminController(Context Context, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.FileSystemManager = fileSystemManager;
		}

		[HttpGet]
		public ActionResult Index()
		{
			return View("Viewer", BuildViewModel());
		}

		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", await BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SOURCE_FOLDER_ID, viewModel.SourceFolder.Id);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_DEFAULT_FILE_ID, viewModel.DefaultFile.Id);

			this.PageModuleManager.SaveSettings(this.Context.Module);

			return Ok();
		}

		private async Task<ViewModels.Viewer> BuildViewModel()
		{
			return await BuildSettingsViewModel<ViewModels.Viewer>(null, this.Context.Site, this.Context.Module, this.FileSystemManager);
		}

		private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			return await BuildSettingsViewModel(viewModel, this.Context.Site, this.Context.Module, this.FileSystemManager);
		}

		internal static async Task<T> BuildSettingsViewModel<T>(T viewModel, Site site, PageModule module, IFileSystemManager fileSystemManager)
			where T: ViewModels.Settings, new()
		{
			if (viewModel == null)
			{
				viewModel = new();

				try
				{
					viewModel.SourceFolder = await fileSystemManager.GetFolder(site, module.ModuleSettings.Get(MODULESETTING_SOURCE_FOLDER_ID, Guid.Empty));
				}
				catch (System.IO.FileNotFoundException)
				{ 
				}

				try
				{
					viewModel.DefaultFile = await fileSystemManager.GetFile(site, module.ModuleSettings.Get(MODULESETTING_DEFAULT_FILE_ID, Guid.Empty));
				}
				catch (System.IO.FileNotFoundException)
				{
				}
			}

			if (viewModel.SourceFolder != null)
			{
				viewModel.SourceFolder = await fileSystemManager.ListFolder(site, viewModel.SourceFolder.Id, "");				
			}

			return viewModel;
		}
	}
}