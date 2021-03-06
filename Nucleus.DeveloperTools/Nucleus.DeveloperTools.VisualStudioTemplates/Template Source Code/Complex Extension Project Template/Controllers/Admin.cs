using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using $nucleus_extension_namespace$.Models;

namespace $nucleus_extension_namespace$.Controllers
{
	[Extension("$nucleus_extension_name$")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
	public class $nucleus_extension_name$AdminController : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private $nucleus_extension_name$Manager $nucleus_extension_name$Manager { get; }

		public $nucleus_extension_name$AdminController(Context Context, IPageModuleManager pageModuleManager, $nucleus_extension_name$Manager $nucleus_extension_name_lcase$Manager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.$nucleus_extension_name$Manager = $nucleus_extension_name_lcase$Manager;			
		}

		[HttpGet]
		[HttpPost]
		public ActionResult Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		[HttpPost]
		public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
		{
			//this.Context.Module.ModuleSettings.Set(MODULESETTING_CATEGORYLIST_ID, viewModel.CategoryList.Id);
			
			await this.PageModuleManager.SaveSettings(this.Context.Module);

			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.ReadSettings(this.Context.Module);

			return viewModel;
		}
	}
}