using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Nucleus.Abstractions.Managers;
using System.IO.Enumeration;
using Nucleus.Extensions;

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
		private Application Application { get; }
		private IPageManager PageManager { get; }
		private ISiteManager SiteManager { get; }

    private IFileSystemManager FileSystemManager { get; }

    private ILogger<PageRoutingMiddleware> Logger { get; }

		private ICacheManager CacheManager { get; }

		private static readonly string[] KNOWN_NON_PAGE_PATHS =
		{
			Nucleus.Abstractions.RoutingConstants.API_ROUTE_PATH,
			Nucleus.Abstractions.RoutingConstants.EXTENSIONS_ROUTE_PATH,
			Nucleus.Abstractions.RoutingConstants.SITEMAP_ROUTE_PATH,
			Nucleus.Abstractions.RoutingConstants.FILES_ROUTE_PATH,
		};

		public PageRoutingMiddleware(ILogger<PageRoutingMiddleware> logger, Context context, Application application, IPageManager pageManager, ISiteManager siteManager, IFileSystemManager fileSystemManager, ICacheManager cacheManager)
		{
			this.Logger = logger;
			this.Context = context;
			this.Application = application;
			this.PageManager = pageManager;
			this.SiteManager = siteManager;
      this.FileSystemManager = fileSystemManager;
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
				Logger.LogTrace("Request for page id {pageid}.", pageId);
				this.Context.Page = await this.PageManager.Get(pageId);

				if (this.Context.Page != null)
				{
					if (this.Context.Page.Disabled)
					{
						Logger.LogTrace("Page id {pageid} is disabled.", pageId);
						this.Context.Page = null;
					}
					else
					{
						Logger.LogTrace("Page id '{pageid}' found.", pageId);
						this.Context.Site = await this.SiteManager.Get(this.Context.Page);
					}

          // When HandleLinkType returns false, it means we should not continue because we are redirecting to another site
          if (!await HandleLinkType(context))
          {
            return;
          }
        }
				else
				{
					Logger.LogTrace("Page id '{pageid}' not found.", pageId);
				}
			}
			else
			{
				if (SkipSiteDetection(context))
				{
					Logger.LogTrace("Skipped site detection for '{request}'.", context.Request.Path);

					this.Context.Site = null;
					await next(context);
					return;
				}

				Logger.LogTrace("Matching site by host '{host}' and pathbase '{pathbase}'.", context.Request.Host, context.Request.PathBase);

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
					string requestedPath = System.Web.HttpUtility.UrlDecode(context.Request.Path);
					Logger.LogTrace("Using site '{siteid}'.", this.Context.Site.Id);

					if (!SkipPageDetection(context))
					{
						Logger.LogTrace("Lookup page by path '{path}'.", requestedPath);

						await FindPage(requestedPath);

            if (this.Context.Page == null)
            {
              // if the page was not found, try searching for path & query
              string requestedPathAndQuery = System.Web.HttpUtility.UrlDecode(context.Request.Path + context.Request.QueryString);

              Logger.LogTrace("Lookup page by path '{path}'.", requestedPathAndQuery);

              await FindPage(requestedPathAndQuery);
            }
          }

					if (this.Context.Page != null)
					{
						if (this.Context.Page.Disabled)
						{
							Logger.LogTrace("Page id '{pageid}' is disabled.", pageId);
							this.Context.Page = null;
						}
            else
						{
							Logger.LogTrace("Page found: '{pageid}'.", this.Context.Page.Id);
						}

            // When HandleLinkType returns false, it means we should not continue because we are redirecting to another site
            if (!await HandleLinkType(context))
            {
              return;
            }
          }
					else
					{
						Logger.LogTrace("Path '{path}' is not a page.", requestedPath);
					}
				}
			}

			await next(context);

			// If the request path did not match a site, and the response is a 404 (so it didn't match a controller route
			// or any other component that can handle the request), and there are no sites in the sites table, redirect to 
			// the setup wizard.  This is to handle cases where there is a /Setup/install-log.config file present (indicating
			// that setup has previously completed), but the database is empty.  This is mostly a scenario that happens in
			// testing, but it could also happen if a user decided to attach to a different (new) database.
			if (context.Response.StatusCode == (int)System.Net.HttpStatusCode.NotFound && this.Context.Site == null)
			{
				if (await this.SiteManager.Count() == 0)
				{
					String relativePath = $"{(String.IsNullOrEmpty(context.Request.PathBase) ? "" : context.Request.PathBase + "/")}Setup/SiteWizard";
					context.Response.Redirect(relativePath);
				}
			}
		}

    private async Task<Boolean> HandleLinkType(HttpContext context)
    {
      switch (this.Context.Page.LinkType)
      {
        case Page.LinkTypes.Normal:
          return true;

        case Page.LinkTypes.Url:
          if (!String.IsNullOrEmpty(this.Context.Page.LinkUrl))
          {
            Logger.LogTrace("Page: '{pageid}' has link type: url.  Redirecting to '{linkUrl}'", this.Context.Page.Id, this.Context.Page.LinkUrl);
            context.Response.Redirect(this.Context.Page.LinkUrl);
            return false;
          }
          else
          {
            Logger.LogTrace("Page: '{pageid}' has link type: Url, but the does not have a LinkPageUrl set.  Treating as a normal page.", this.Context.Page.Id);
            break;
          }

        case Page.LinkTypes.Page:
          if (this.Context.Page.LinkPageId.HasValue)
          {
            this.Context.Page = await this.PageManager.Get(this.Context.Page.LinkPageId.Value);
            Logger.LogTrace("Page: '{pageid}' has link type: Page.  Using page id '{linkPageId}'", this.Context.Page.Id, this.Context.Page.LinkPageId);
          }
          else
          {
            Logger.LogTrace("Page: '{pageid}' has link type: Page, but the does not have a LinkPageId set.  Treating as a normal page.", this.Context.Page.Id);
          }
          break;

        case Page.LinkTypes.File:
          if (this.Context.Page.LinkFileId.HasValue)
          {
            Nucleus.Abstractions.Models.FileSystem.File file = await this.FileSystemManager.GetFile(this.Context.Site, this.Context.Page.LinkFileId.Value);
            context.Response.Redirect($"{(String.IsNullOrEmpty(context.Request.PathBase) ? "" : context.Request.PathBase + "/")}/{Nucleus.Abstractions.RoutingConstants.FILES_ROUTE_PATH}/{file.EncodeFileId()}");
            Logger.LogTrace("Page: '{pageid}' has link type: File.  Using file id '{linkFileId}'", this.Context.Page.Id, this.Context.Page.LinkFileId);
            return false;
          }
          else
          {
            Logger.LogTrace("Page: '{pageid}' has link type: File, but the does not have a LinkFileId set.  Treating as a normal page.", this.Context.Page.Id);
          }
          break;
      }

      return true;
    }

		/// <summary>
		/// Find a page for the requested path, populating and reading from the Page Route cache.
		/// </summary>
		/// <param name="requestPath"></param>
		/// <returns></returns>
		/// <remarks>
		/// This function includes logic to partly match the requested path, and set Context.LocalPath to the 
		/// unmatched portion of the path, and also caches partly-matched paths.
		/// </remarks>
		private async Task FindPage(string requestPath)
		{			
			string pagePathCacheKey = (this.Context.Site.Id.ToString() + "^" + requestPath).ToLower();

			FoundPage found = await this.CacheManager.PageRouteCache().GetAsync(pagePathCacheKey, async pagePathCacheKey =>
			{
				Page page = await this.PageManager.Get(this.Context.Site, requestPath);

				if (page != null)
				{
					return new() { Page = page, RequestPath = requestPath, MatchedRoute = GetFoundRoute(page, requestPath) };					
				}

				string partPath = requestPath;
				string parameters = "";

				while (this.Context.Page == null && !String.IsNullOrEmpty(partPath))
				{
					int lastIndexOfSeparator = partPath.LastIndexOfAny(new char[] { '/', '&', '?' });
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
						page = await this.PageManager.Get(this.Context.Site, partPath);
						if (page != null)
						{
							return new() { Page = page, LocalPath = new(parameters), RequestPath = requestPath, MatchedRoute = GetFoundRoute(page, partPath)};							
						}
					}
				}

				return null;
			});
		
			if (found != null)
			{
				this.Context.Page = found.Page;
				this.Context.LocalPath = found.LocalPath;
        this.Context.MatchedRoute = found.MatchedRoute;
			}		
		}

    private PageRoute GetFoundRoute(Page page, string matchedPath)
    {
      foreach (PageRoute pageRoute in page.Routes.ToArray())
      {
        if (pageRoute.Path.Equals(matchedPath, StringComparison.OrdinalIgnoreCase))
        {
          return pageRoute;
        }
      }
      return page.DefaultPageRoute();
    }

		/// <summary>
		/// Gets whether to skip site detection
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// Skip attempt to identify the current site when the http request is for:					
		///  - The setup wizard.  This is mainly to avoid a database connection error before the wizard can do 
		///		 configuration checks and report problems to the user.
		///  - The error controller. If an error occurs trying to read page data for the error page, then it is most likely a database connection error.
		///    Suppress the exception so that the error handler (Nucleus.Web.Controllers.Error) can handle the original error and
		///    report it as a plain-text error.
		///  - favicon.ico
		/// </remarks>
		private Boolean SkipSiteDetection(HttpContext context)
		{
			// Browsers often send a request for /favicon.ico even when the page doesn't specify an icon
			if (context.Request.Path.Value.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
					
			if (context.Request.Path.Value.StartsWith("/Setup/SiteWizard", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (context.Request.Path.Value.StartsWith("/" + Nucleus.Abstractions.RoutingConstants.ERROR_ROUTE_PATH, StringComparison.OrdinalIgnoreCase))
			{
				// We need to detect the site when using the custom error route so that ErrorController has a Context.Site to read
				// the selected error page id from (so return false) - but NOT if there has been a database connection error (so return
				// true to prevent site detection, since we aren't going to be able to read the database)			
				if (context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error is Nucleus.Data.Common.ConnectionException == true)
				{ 
					return true;
				}
			}

			if (!this.Application.IsInstalled)
			{
				return true;
			}

			return false;	
		}

		/// <summary>
		/// Gets whether to skip page detection
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// Skip attempt to identify the current page when the http request is for:					
		///  - A file
		/// </remarks>
		private Boolean SkipPageDetection(HttpContext context)
		{
			if (context.Request.Path.HasValue && context.Request.Path != "/")
			{
				// skip page detection when the request is for a known reserved path.
				foreach (string knownPath in KNOWN_NON_PAGE_PATHS)
				{
					if (context.Request.Path.StartsWithSegments("/" + knownPath))
					{
						return true;
					}
				}
				//// skip page detection when the request is for a known reserved path.  The Substring[1..] is to skip the leading "/" which
				//// is always present.
				//if (KNOWN_NON_PAGE_PATHS.Contains(context.Request.Path.Value[1..], StringComparer.OrdinalIgnoreCase))
				//{
				//	return true;
				//}
			}

			return false;
		}
	}
}
