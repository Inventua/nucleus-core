using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Core;
using Nucleus.Core.Authorization;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.FileSystemProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.ViewFeatures;
using Nucleus.Modules.Links.Models;

namespace Nucleus.Modules.Links.Controllers
{
	[Extension("Links")]
	public class LinksController : Controller
	{
		private Context Context { get; }
		private PageModuleManager PageModuleManager { get; }
		private LinksManager LinksManager { get; }
		private ListManager ListManager { get; }
		private IWebHostEnvironment WebHostEnvironment { get; }

		public LinksController(IWebHostEnvironment webHostEnvironment, Context Context, PageModuleManager pageModuleManager, LinksManager linksManager, ListManager listManager)
		{
			this.WebHostEnvironment = webHostEnvironment;
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.LinksManager = linksManager;
			this.ListManager = listManager;
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

			viewModel.Links = new();
			foreach (Link link in this.LinksManager.List(this.Context.Site, this.Context.Module))
			{
				Boolean isValid = true;

				// Check permissions
				switch (link.LinkType)
				{
					case LinkTypes.File:
						if (!HttpContext.User.HasViewPermission(this.Context.Site, link.LinkFile.File.Parent))
						{
							isValid = false;
						}
						break;
					case LinkTypes.Page:
						if (link.LinkPage.Page.Disabled || !HttpContext.User.HasViewPermission(this.Context.Site, link.LinkPage.Page))
						{
							isValid = false;
						}
						break;
				}

				if (isValid)
				{
					viewModel.Links.Add(link);
				}
			}

			viewModel.Layout = $"ViewerLayouts/{this.Context.Module.ModuleSettings.Get(AdminController.MODULESETTING_LAYOUT, "Table")}.cshtml";
			viewModel.CategoryList = this.ListManager.Get(this.Context.Module.ModuleSettings.Get(AdminController.MODULESETTING_CATEGORYLIST_ID, Guid.Empty));
			
			return viewModel;
		}

		private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.CategoryList = this.ListManager.Get(this.Context.Module.ModuleSettings.Get(AdminController.MODULESETTING_CATEGORYLIST_ID, Guid.Empty));
			viewModel.Layout = this.Context.Module.ModuleSettings.Get(AdminController.MODULESETTING_LAYOUT, "Table");
			viewModel.Lists = this.ListManager.List(this.Context.Site);

			viewModel.ModuleId = this.Context.Module.Id;
			viewModel.Links = this.LinksManager.List(this.Context.Site, this.Context.Module);


			viewModel.Layouts = new();
			foreach (string file in System.IO.Directory.EnumerateFiles($"{this.WebHostEnvironment.ContentRootPath}\\{Constants.EXTENSIONS_ROUTE_PATH}\\Links\\Views\\ViewerLayouts\\", "*.cshtml").OrderBy(layout => layout))
			{
				viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file));
			}

			return viewModel;
		}

	}
}