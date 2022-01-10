using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core.Layout
{
	/// <summary>
	/// Provides support for page routing.  Pages can have any but the reserved list of routes (urls), this class checks the
	/// request Url and matches it to a page, and sets the context.Page property to the matching page.  
	/// 
	/// For API and troubleshooting purposes, a route with a "pageid" querystring value can be specified, in this case the 
	/// page is selected by Id.  A pageId querystring value takes precendence over the route (request path) for the purposes of page selection.
	/// </summary>
	public class PageRoutingMiddleware : Microsoft.AspNetCore.Http.IMiddleware
	{		
		private Context Context { get; }
		private IPageManager PageManager { get; }
		private ISiteManager SiteManager { get; }
		private ILogger<PageRoutingMiddleware> Logger { get; }

		private ICacheManager CacheManager { get; }

		public PageRoutingMiddleware(ILogger<PageRoutingMiddleware> logger, Context context, IPageManager pageManager, ISiteManager siteManager, ICacheManager cacheManager)
		{
			this.Logger = logger;
			this.Context = context;
			this.PageManager = pageManager;
			this.SiteManager = siteManager;
			this.CacheManager = cacheManager;
		}

		/// <summary>
		/// Handles an incoming request by matching the request local path to a page, or if the request contains a "pageid" query string value, 
		/// use that value to match a page by Id.  This method sets the Nucleus context (module, page and site) for the requested page.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="next"></param>
		/// <returns></returns>
		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
		{

			if (Guid.TryParse(context.Request.Query["pageid"], out Guid pageId))
			{
				Logger.LogTrace("Request for page id {0}.", pageId);
				this.Context.Page = await this.PageManager.Get(pageId);

				if (this.Context.Page != null)
				{
					if (this.Context.Page.Disabled)
					{
						Logger.LogTrace("Page id {0} is disabled.", pageId);
						this.Context.Page = null;
					}
					else
					{
						Logger.LogTrace("Page id {0} found.", pageId);
						this.Context.Site = await this.SiteManager.Get(this.Context.Page);
					}
				}
				else
				{
					Logger.LogTrace("Page id not {0} found.", pageId);
				}
			}
			else
			{
				Logger.LogTrace("Matching site by host {0} and pathbase {1}.", context.Request.Host, context.Request.PathBase);

				this.Context.Site = await this.SiteManager.Get(context.Request.Host, context.Request.PathBase);

				if (this.Context.Site == null)
				{
					Logger.LogTrace("Using default site.");
					this.Context.Site = await this.SiteManager.Get(new HostString(""), "");

					if (this.Context.Site != null)
					{
						// Add "default" site to the site alias table 
						SiteAlias alias = new() { Alias = $"{context.Request.Host}{context.Request.PathBase}" };
						await this.SiteManager.SaveAlias(this.Context.Site, alias);

						// If the site doesn't already have a default alias, set it to the new alias
						if (this.Context.Site.DefaultSiteAlias == null)
						{
							this.Context.Site.DefaultSiteAlias = alias;
							await this.SiteManager.Save(this.Context.Site);
						}
					}
				}

				if (this.Context.Site != null)
				{
					string localPath = System.Web.HttpUtility.UrlDecode(context.Request.Path);

					Logger.LogTrace("Using site {0}.", this.Context.Site.Id);
					Logger.LogTrace("Lookup page by path {0}.", localPath);
					this.Context.Page = await this.PageManager.Get(this.Context.Site, localPath);

					string partPath = localPath;
					string parameters = "";

					while (this.Context.Page == null && !String.IsNullOrEmpty(partPath))
					{
						int lastIndexOfSeparator = partPath.LastIndexOf('/');
						string nextParameterPart = partPath[(lastIndexOfSeparator + 1)..];
						if (nextParameterPart.Length > 0)
						{
							if (!String.IsNullOrEmpty(parameters))
							{
								parameters = nextParameterPart + "/" + parameters;
							}
							else
							{
								parameters = nextParameterPart;
							}
						}

						partPath = partPath.Substring(0, lastIndexOfSeparator);

						if (!String.IsNullOrEmpty(partPath))
						{
							this.Context.Page = await this.PageManager.Get(this.Context.Site, partPath);
							if (this.Context.Page != null)
							{
								this.Context.Parameters = parameters;
							}
						}
					}

					if (this.Context.Page != null)
					{
						if (this.Context.Page.Disabled)
						{
							Logger.LogTrace("Page id {0} is disabled.", pageId);
							this.Context.Page = null;
						}
						else
						{
							Logger.LogTrace("Page found: {0}.", this.Context.Page.Id);
						}
					}
					else
					{
						Logger.LogTrace("Path {0} is not a page.", localPath);
					}
				}
			}

			await next(context);
			
		}
	}
}
