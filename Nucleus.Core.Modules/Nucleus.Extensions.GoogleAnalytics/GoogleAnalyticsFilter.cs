﻿using System;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Routing;
using Nucleus.ViewFeatures;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Http;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Extensions.GoogleAnalytics
{
	public class GoogleAnalyticsFilter : Microsoft.AspNetCore.Mvc.Filters.IAsyncResultFilter
	{
		private HtmlHelper HtmlHelper { get; }
		private IUrlHelperFactory UrlHelperFactory { get; }
		private Context Context { get; }

		public GoogleAnalyticsFilter(IUrlHelperFactory urlHelperFactory, Context context)
		{
			this.UrlHelperFactory = urlHelperFactory;
			this.Context = context;
		}

		public async Task OnResultExecutionAsync(ResultExecutingContext executingContext, ResultExecutionDelegate next)
		{
			ViewResult result = executingContext.Result as ViewResult;
			Boolean excludeAdmins = true;

			if (this.Context.Site != null)
			{
				// Only render the Google Analytics scripts when a page is requested (not when rendering admin views, partial renders, or other resources).
				if (result != null && result.Model != null && result.Model is Nucleus.ViewFeatures.ViewModels.Layout)
				{
					if (this.Context.Site.SiteSettings.TryGetValue(Controllers.GoogleAnalyticsController.SETTING_ANALYTICS_ID, out string googleAnalyticsId))
					{
						this.Context.Site.SiteSettings.TryGetValue(Controllers.GoogleAnalyticsController.SETTING_EXCLUDE_ADMINISTRATORS, out excludeAdmins);

						// IsSiteAdmin returns true for both site admins and system admins
						if (!String.IsNullOrEmpty(googleAnalyticsId) && (!excludeAdmins || !executingContext.HttpContext.User.IsSiteAdmin(this.Context.Site)))
						{
							IUrlHelper urlHelper = this.UrlHelperFactory.GetUrlHelper(executingContext);

							// Use the existing AddScript HtmlHelper to render the Google Analytics scripts.
							Nucleus.ViewFeatures.HtmlHelpers.AddScriptHtmlHelper.AddScript(executingContext.HttpContext, $"https://www.googletagmanager.com/gtag/js?id={googleAnalyticsId}", true, 1000);
							
							// Render the script (RenderGoogleAnalyticsScript action) 
							// The id querystring value is present as a "cache buster", in case the user changes their analytics id
							Nucleus.ViewFeatures.HtmlHelpers.AddScriptHtmlHelper.AddScript(executingContext.HttpContext,  urlHelper.NucleusAction("RenderGoogleAnalyticsScript", "GoogleAnalytics", "GoogleAnalytics") + $"?id={googleAnalyticsId}", false, 1001);
						}
					}
				}
			}

			await next();
		}
	}
}
