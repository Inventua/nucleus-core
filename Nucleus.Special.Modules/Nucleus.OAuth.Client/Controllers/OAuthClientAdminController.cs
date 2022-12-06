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
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace Nucleus.OAuth.Client.Controllers
{
	[Extension("OAuthClient")]
	public class OAuthClientAdminController : Controller
	{
    private FolderOptions FolderOptions { get; }

    private Context Context { get; }
		
		private ISiteManager SiteManager { get; }

		private IPageModuleManager PageModuleManager { get; }

		private IOptions<Models.Configuration.OAuthProviders> Options { get; }


		public OAuthClientAdminController(Context Context, IOptions<FolderOptions> folderOptions, ISiteManager siteManager, IPageModuleManager pageModuleManager, IOptions<Models.Configuration.OAuthProviders> options)
		{
			this.Context = Context;
      this.FolderOptions = folderOptions.Value;
      this.SiteManager = siteManager;
			this.PageModuleManager = pageModuleManager;
			this.Options = options;
		}

		//private string BuildRedirectUrl(string returnUrl)
		//{
		//	// Only allow a relative path for redirectUri (that is, the url must start with "/"), to ensure that it points to "this" site.					
		//	return String.IsNullOrEmpty(returnUrl) || !returnUrl.StartsWith("/") ? "~/" : returnUrl;
		//}
		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public ActionResult Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", BuildSettingsViewModel(viewModel));
		}


		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
		[HttpGet]
		[HttpPost]
		public ActionResult SiteSettings(ViewModels.SiteClientSettings viewModel)
		{
			return View("SiteClientSettings", BuildSiteClientSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
		[HttpPost]
		public async Task<ActionResult> SaveSiteSettings(ViewModels.SiteClientSettings viewModel)
		{
			this.Context.Site.SiteSettings.TrySetValue(ViewModels.SiteClientSettings.SETTING_MATCH_BY_NAME, viewModel.MatchByName);
			this.Context.Site.SiteSettings.TrySetValue(ViewModels.SiteClientSettings.SETTING_MATCH_BY_EMAIL, viewModel.MatchByEmail);

			this.Context.Site.SiteSettings.TrySetValue(ViewModels.SiteClientSettings.SETTING_CREATE_USERS, viewModel.CreateUsers);
			this.Context.Site.SiteSettings.TrySetValue(ViewModels.SiteClientSettings.SETTING_AUTO_VERIFY, viewModel.AutomaticallyVerifyNewUsers);
			this.Context.Site.SiteSettings.TrySetValue(ViewModels.SiteClientSettings.SETTING_AUTO_APPROVE, viewModel.AutomaticallyApproveNewUsers);

			this.Context.Site.SiteSettings.TrySetValue(ViewModels.SiteClientSettings.SETTING_SYNC_ROLES, viewModel.SynchronizeRoles);
			this.Context.Site.SiteSettings.TrySetValue(ViewModels.SiteClientSettings.SETTING_ADD_ROLES, viewModel.AddToRoles);
			this.Context.Site.SiteSettings.TrySetValue(ViewModels.SiteClientSettings.SETTING_REMOVE_ROLES, viewModel.RemoveFromRoles);

			this.Context.Site.SiteSettings.TrySetValue(ViewModels.SiteClientSettings.SETTING_SYNC_PROFILE, viewModel.SynchronizeProfile);

			await this.SiteManager.Save(this.Context.Site);

			return Ok();
		}


		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Module.ModuleSettings.Set(ViewModels.Settings.MODULESETTING_CAPTION, viewModel.Caption);
			this.Context.Module.ModuleSettings.Set(ViewModels.Settings.MODULESETTING_AUTOLOGIN, viewModel.AutoLogin);
			this.Context.Module.ModuleSettings.Set(ViewModels.Settings.MODULESETTING_LAYOUT, viewModel.Layout);

			await this.PageModuleManager.SaveSettings(this.Context.Module);

			return Ok();
		}

		private ViewModels.SiteClientSettings BuildSiteClientSettingsViewModel(ViewModels.SiteClientSettings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.ReadSettings(this.Context.Site);

			return viewModel;
		}


		private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.ReadSettings(this.Context.Module);

			viewModel.Layouts = new();
			foreach (string file in System.IO.Directory.EnumerateFiles($"{this.FolderOptions.GetExtensionFolder("OAuth Client", false)}/Views/ViewerLayouts/", "*.cshtml").OrderBy(layout => layout))
			{
				viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file));
			}

			return viewModel;
		}

	}
}