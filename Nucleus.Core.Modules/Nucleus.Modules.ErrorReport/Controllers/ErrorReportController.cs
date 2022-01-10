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
using Microsoft.AspNetCore.Diagnostics;

namespace Nucleus.Modules.ErrorReport.Controllers
{
	[Extension("ErrorReport")]
	public class ErrorReportController : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }

		public ErrorReportController(Context Context, IPageModuleManager pageModuleManager)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
		}

		[HttpGet]
		public ActionResult Index()
		{
			IExceptionHandlerFeature exceptionDetails = ControllerContext.HttpContext.Features.Get<IExceptionHandlerFeature>();
			
			return View("Viewer", BuildViewModel(exceptionDetails));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public ActionResult Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public ActionResult SaveSettings(ViewModels.Settings viewModel)
		{
			//this.Context.Module.ModuleSettings.Set(MODULESETTING_CATEGORYLIST_ID, viewModel.CategoryList.Id);

			this.PageModuleManager.SaveSettings(this.Context.Module);

			return Ok();
		}

		private ViewModels.Viewer BuildViewModel(IExceptionHandlerFeature exceptionInfo)
		{
			ViewModels.Viewer viewModel = new();
			viewModel.ExceptionInfo = exceptionInfo;
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