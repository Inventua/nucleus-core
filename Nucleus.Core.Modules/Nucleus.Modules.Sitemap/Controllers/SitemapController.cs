using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace Nucleus.Modules.Sitemap.Controllers
{
	[Extension("SiteMap")]
	public class SitemapController : Controller
	{
		private Context Context { get; }
		private IPageManager PageManager { get; }
		private IPageModuleManager PageModuleManager { get; }

		private class ModuleSettingsKeys
		{
			public const string SETTINGS_MAXLEVELS = "sitemap:maxlevels";
			public const string SETTINGS_ROOTPAGE_TYPE = "sitemap:root-page-type";
			public const string SETTINGS_ROOTPAGE = "sitemap:root-page";
			public const string SETTINGS_SHOWDESCRIPTION = "sitemap:show-description";
		}

		public SitemapController(Context context, IPageManager pageManager, IPageModuleManager pageModuleManager)
		{
			this.Context = context;
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
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		public async Task<ActionResult> Edit()
		{
			ViewResult result = View("Settings", await BuildSettingsModel());
			return result;
		}

		[HttpGet]
		public async Task<ActionResult> GetChildPages(Guid id)
		{
			ViewModels.PageIndexPartial viewModel = new();

			viewModel.FromPage = await this.PageManager.Get(id);

			viewModel.Pages = await this.PageManager.GetAdminMenu
				(
					this.Context.Site,
					await this.PageManager.Get(id),
					ControllerContext.HttpContext.User,
					1
				);

			return View("_PageMenu", viewModel);
		}

		private ViewModels.Sitemap BuildViewModel()
		{
			ViewModels.Sitemap viewModel = new ViewModels.Sitemap();

			if (this.Context.Module != null)
			{
				viewModel.MaxLevels = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_MAXLEVELS, 0);
				viewModel.RootPageType = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_ROOTPAGE_TYPE, Nucleus.Modules.Sitemap.RootPageTypes.SelectedPage);
				viewModel.RootPageId = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_ROOTPAGE, Guid.Empty);
				viewModel.ShowDescription = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_SHOWDESCRIPTION, false);
			}

			return viewModel;
		}

		private async Task<ViewModels.SitemapSettings> BuildSettingsModel()
		{
			ViewModels.SitemapSettings viewModel = new ViewModels.SitemapSettings();

			if (this.Context.Module != null)
			{
				viewModel.MaxLevels = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_MAXLEVELS, 0);
				viewModel.RootPageType = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_ROOTPAGE_TYPE, Nucleus.Modules.Sitemap.RootPageTypes.SelectedPage);
				viewModel.RootPageId = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_ROOTPAGE, Guid.Empty);
				viewModel.ShowDescription = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.SETTINGS_SHOWDESCRIPTION, false);
			}

			viewModel.Pages = await this.PageManager.GetAdminMenu(this.Context.Site, await this.PageManager.Get(viewModel.RootPageId), HttpContext.User, 1);

			return viewModel;
		}

		[HttpPost]
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		public async Task<ActionResult> Save(ViewModels.Sitemap viewModel)
		{
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.SETTINGS_MAXLEVELS, viewModel.MaxLevels);
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.SETTINGS_ROOTPAGE_TYPE, viewModel.RootPageType);
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.SETTINGS_ROOTPAGE, viewModel.RootPageId);
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.SETTINGS_SHOWDESCRIPTION, viewModel.ShowDescription);

			await this.PageModuleManager.SaveSettings(this.Context.Module);
			
			return Ok();
		}
	}
}
