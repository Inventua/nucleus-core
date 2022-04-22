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
		private IWebHostEnvironment WebHostEnvironment { get; }

		private Context Context { get; }
		
		private ISiteManager SiteManager { get; }

		private IPageModuleManager PageModuleManager { get; }

		private IOptions<Models.Configuration.OAuthProviders> Options { get; }


		public OAuthClientAdminController(IWebHostEnvironment webHostEnvironment, Context Context, ISiteManager siteManager, IPageModuleManager pageModuleManager, IOptions<Models.Configuration.OAuthProviders> options)
		{
			this.WebHostEnvironment = webHostEnvironment;
			this.Context = Context;
			this.SiteManager = siteManager;
			this.PageModuleManager = pageModuleManager;
			this.Options = options;
		}

		private string BuildRedirectUrl(string returnUrl)
		{
			// Only allow a relative path for redirectUri (that is, the url must start with "/"), to ensure that it points to "this" site.					
			return String.IsNullOrEmpty(returnUrl) || !returnUrl.StartsWith("/") ? "/" : returnUrl;
		}


		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
		[HttpGet]
		[HttpPost]
		public ActionResult Settings(ViewModels.SiteClientSettings viewModel)
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

		private ViewModels.SiteClientSettings BuildSiteClientSettingsViewModel(ViewModels.SiteClientSettings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.ReadSettings(this.Context.Site);

			return viewModel;
		}

	}
}