using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Core;
using Nucleus.Core.FileSystemProviders;
using Nucleus.Abstractions.Models.FileSystem;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Core.Authorization;
using Microsoft.AspNetCore.Http;
using Nucleus.Core.DataProviders.Abstractions;
using Microsoft.AspNetCore.Hosting;

namespace $nucleus_extension_namespace$.Controllers
{
	[Extension("$nucleus_extension_name$")]
	public class $nucleus_extension_name$Controller : Controller
	{
		private Context Context { get; }
		private PageModuleManager PageModuleManager { get; }
		
		public $nucleus_extension_name$Controller(Context Context, PageModuleManager pageModuleManager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;		
		}

		[HttpGet]
		public ActionResult Index()
		{
			return View("Viewer", BuildViewModel());
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public ActionResult Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult SaveSettings(ViewModels.Settings viewModel)
		{
			//this.Context.Module.ModuleSettings.Set(MODULESETTING_CATEGORYLIST_ID, viewModel.CategoryList.Id);
			
			this.PageModuleManager.SaveSettings(this.Context.Module);

			viewModel.Message = "Changes Saved.";
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		private ViewModels.Viewer BuildViewModel()
		{
			ViewModels.Viewer viewModel = new();
			//viewModel.ShowSize = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SIZE, true);
			return viewModel;
		}

		private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			//viewModel.ModuleId = this.Context.Module.Id;

			return viewModel;
		}

	}
}