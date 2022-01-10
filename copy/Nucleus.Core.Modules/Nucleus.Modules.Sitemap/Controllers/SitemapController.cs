using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;
using Nucleus.Core;

namespace Nucleus.Modules.Sitemap.Controllers
{
	[Extension("SiteMap")]
	public class SitemapController : Controller
	{
		private Context Context { get; }
		private PageManager PageManager { get; }
		private PageModuleManager PageModuleManager { get; }

		private class ModuleSettingsKeys
		{
			public const string SETTINGS_MAXLEVELS = "sitemap:maxlevels";
			public const string SETTINGS_ROOTPAGE_TYPE = "sitemap:root-page-type";
			public const string SETTINGS_ROOTPAGE = "sitemap:root-page";
			public const string SETTINGS_SHOWDESCRIPTION = "sitemap:show-description";
		}

		public SitemapController(Context Context, PageManager pageManager, PageModuleManager pageModuleManager)
		{
			this.Context = Context;
			this.PageManager = pageManager; 
			this.PageModuleManager = pageModuleManager;			
		}

		[HttpGet]
		public ActionResult Index()
		{
			ViewResult result = View("Viewer", BuildViewModel());			
			return result;
		}

		[HttpPost]
		public ActionResult Edit()
		{
			ViewResult result = View("Settings", BuildViewModel());
			return result;
		}

		private ViewModels.Sitemap BuildViewModel()
		{
			// http://www.inventua.com/Default.aspx?tabid=139
			ViewModels.Sitemap viewModel = new ViewModels.Sitemap();

			if (this.Context.Module != null)
			{
				viewModel.MaxLevels = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_MAXLEVELS, 0);
				viewModel.RootPageType = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_ROOTPAGE_TYPE, Nucleus.Modules.Sitemap.RootPageTypes.SelectedPage);
				viewModel.RootPageId = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_ROOTPAGE, Guid.Empty);
				viewModel.ShowDescription = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_SHOWDESCRIPTION, false);
			}

			viewModel.Pages = this.PageManager.GetAdminMenu(this.Context.Site, HttpContext.User);

			return viewModel;
		}

		[HttpPost]
		public ActionResult Save(ViewModels.Sitemap viewModel)
		{
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.SETTINGS_MAXLEVELS, viewModel.MaxLevels);
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.SETTINGS_ROOTPAGE_TYPE, viewModel.RootPageType);
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.SETTINGS_ROOTPAGE, viewModel.RootPageId);
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.SETTINGS_SHOWDESCRIPTION, viewModel.ShowDescription);

			this.PageModuleManager.SaveSettings(this.Context.Module);
			
			return Ok();
		}
	}
}
