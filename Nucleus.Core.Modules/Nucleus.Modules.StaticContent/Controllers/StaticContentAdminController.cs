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
using Microsoft.AspNetCore.Http;

namespace Nucleus.Modules.StaticContent.Controllers
{
	[Extension("StaticContent")]
	public class StaticContentAdminController : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private IFileSystemManager FileSystemManager { get; }
		private ICacheManager CacheManager { get; }

		public StaticContentAdminController(Context Context, ICacheManager cacheManager, IPageModuleManager pageModuleManager, IFileSystemManager fileSystemManager)
		{
			this.Context = Context;
			this.CacheManager = cacheManager;
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
		public ActionResult SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_DEFAULT_FILE_ID, viewModel.DefaultFile.Id);
			this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_ADD_COPY_BUTTONS, viewModel.AddCopyButtons);

			this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

			this.CacheManager.StaticContentCache().Clear();

			return Ok();
		}

		private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}
			viewModel.ReadSettings(this.Context.Module);

			if (viewModel.DefaultFile == null)
			{
				if (viewModel.DefaultFileId != Guid.Empty)
				{
					try
					{
						viewModel.DefaultFile = await this.FileSystemManager.GetFile(this.Context.Site, viewModel.DefaultFileId);
					}
					catch (System.IO.FileNotFoundException)
					{
						viewModel.DefaultFile = null;
					}
				}
			}

			return viewModel;
		}

    [HttpPost]
		public ActionResult SelectAnother(ViewModels.Settings viewModel)
		{
			viewModel.DefaultFile.ClearSelection();

			return View("Settings", viewModel); 
		}

		[HttpPost]
		public async Task<ActionResult> UploadFile(ViewModels.Settings viewModel, [FromForm] IFormFile mediaFile)
		{
			if (mediaFile != null)
			{
				viewModel.DefaultFile.Parent = await this.FileSystemManager.GetFolder(this.Context.Site, viewModel.DefaultFile.Parent.Id);
				using (System.IO.Stream fileStream = mediaFile.OpenReadStream())
				{
					viewModel.DefaultFile = await this.FileSystemManager.SaveFile(this.Context.Site, viewModel.DefaultFile.Provider, viewModel.DefaultFile.Parent.Path, mediaFile.FileName, fileStream, false);
				}
			}
			else
			{
				return BadRequest();
			}

			return View("Settings", viewModel);
		}
	}
}