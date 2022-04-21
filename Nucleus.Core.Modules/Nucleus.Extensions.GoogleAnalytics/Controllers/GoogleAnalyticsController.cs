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

namespace Nucleus.Extensions.GoogleAnalytics.Controllers
{
	[Extension("GoogleAnalytics")]
	public class GoogleAnalyticsController : Controller
	{
		private Context Context { get; }
		private ISiteManager SiteManager { get; }

		internal const string SETTING_ANALYTICS_ID = "googleanalytics:id";

		public GoogleAnalyticsController(Context Context, ISiteManager siteManager)
		{
			this.Context = Context;
			this.SiteManager = siteManager;
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
		[HttpGet]
		[HttpPost]
		public ActionResult Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
		[HttpPost]
		public ActionResult SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Site.SiteSettings.TrySetValue(SETTING_ANALYTICS_ID, viewModel.GoogleAnalyticsId);
			this.SiteManager.Save(this.Context.Site);

			return Ok();
		}

		[HttpGet]
		public ActionResult RenderGoogleAnalyticsScript()
		{
			ControllerContext.HttpContext.Response.ContentType = "text/javascript";
			return View("AnalyticsScript", BuildSettingsViewModel(null));
		}

		private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			if (this.Context.Site.SiteSettings.TryGetValue(SETTING_ANALYTICS_ID, out string googleId))
			{
				viewModel.GoogleAnalyticsId = googleId;
			}

			return viewModel;
		}

	}
}