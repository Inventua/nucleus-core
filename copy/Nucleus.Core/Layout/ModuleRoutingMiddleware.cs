using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core;
using Nucleus.Core.Authorization;

namespace Nucleus.Core.Layout
{
	/// <summary>
	/// Provides support for API (/api/modules/[module]/[action]) routes where the request path contains a "mid" querystring value by finding the 
	/// corresponding module and populating the context object's page and module properties.  The context object is a scoped DI object.
	/// </summary>
	public class ModuleRoutingMiddleware : Microsoft.AspNetCore.Http.IMiddleware
	{
		private Context Context { get; }
		private PageManager PageManager { get; }
		private PageModuleManager PageModuleManager { get; }
		private SiteManager SiteManager { get; }
		private ILogger<ModuleRoutingMiddleware> Logger { get; }

		public ModuleRoutingMiddleware(ILogger<ModuleRoutingMiddleware> logger, Context context, PageManager pageManager, PageModuleManager pageModuleManager, SiteManager siteManager)
		{
			this.Logger = logger;
			this.Context = context;
			this.PageManager = pageManager;
			this.PageModuleManager = pageModuleManager;
			this.SiteManager = siteManager;
		}

		/// <summary>
		/// Handles an incoming request by checking whether the request contains a "mid" query string value and setting the Nucleus
		/// context (module, page and site) for the requested module.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="next"></param>
		/// <returns></returns>
		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
		{
			Guid moduleId;
			PageModule moduleInfo;
			Site site = null;
			Page page = null;
			string mid = null;

			if (context.Request.RouteValues.ContainsKey("mid"))
			{
				mid = context.Request.RouteValues["mid"].ToString();
			}
			else if (context.Request.Query.ContainsKey("mid"))
			{
				mid = context.Request.Query["mid"];	
			}
			
			if (mid != null && Guid.TryParse(mid, out moduleId))
			{
				Logger.LogTrace("Request for module id {0}", moduleId);

				moduleInfo = this.PageModuleManager.Get(moduleId);

				if (moduleInfo != null)
				{
					page = this.PageManager.Get(moduleInfo);
				}

				if (page != null)
				{
					site = this.SiteManager.Get(page);
				}
								
				if (site != null && page != null && moduleInfo != null && (context.User.HasViewPermission(site, page, moduleInfo) ||  context.User.HasEditPermission(site, page, moduleInfo)))
				{
					if (moduleInfo != null)
					{
						Logger.LogTrace("Module id {0} found.", moduleId);
						
						this.Context.Module = moduleInfo;
						this.Context.Page = page;
						this.Context.Site = site;
					}
					else
					{
						Logger.LogTrace("Module id {0} not found.", moduleId);
					}
				}
				else
				{
					Logger.LogTrace("Permission denied for module id {0}, user {1}.", moduleId, context.User.GetUserId());
					context.Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
					return;
				}
			}

			await next(context);
		}
	}
}
