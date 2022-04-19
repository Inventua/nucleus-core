using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.OAuth.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.OAuth.Server.Controllers
{
	[Extension("OAuthServer")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
	public class OAuthServerAdminController : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private ClientAppManager ClientAppManager { get; }

		public OAuthServerAdminController(Context Context, IPageModuleManager pageModuleManager, ClientAppManager clientAppManager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.ClientAppManager = clientAppManager;
		}

		[HttpGet]
		[HttpPost]
		public ActionResult Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		[HttpGet]
		[HttpPost]
		public ActionResult Editor(Guid id)
		{
			return View("Editor", BuildSettingsViewModel(id));
		}

		[HttpPost]
		public ActionResult SaveSettings(ViewModels.Settings viewModel)
		{
			//this.Context.Module.ModuleSettings.Set(MODULESETTING_CATEGORYLIST_ID, viewModel.CategoryList.Id);

			this.PageModuleManager.SaveSettings(this.Context.Module);

			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.ClientApps = await this.ClientAppManager.List(this.Context.Site, viewModel.ClientApps);

			return viewModel;
		}

		private async Task<ViewModels.Settings> BuildSettingsViewModel(Guid id)
		{
			ViewModels.Settings viewModel = new();

			viewModel.ClientApp = await this.ClientAppManager.Get(id);

			return viewModel;
		}

	}
}