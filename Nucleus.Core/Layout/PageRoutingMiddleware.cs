using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Nucleus.Core.Layout
{
  /// <summary>
  /// Provides support for page routing.  Pages can have multiple routes, with any values except for the reserved list of routes in 
  /// KNOWN_NON_PAGE_PATHS. This class checks the request Url and matches it to a page, and sets the context.Page property to the matching page.  
  /// 
  /// For API and troubleshooting purposes, a route with a "pageid" querystring value can be specified, in this case the 
  /// page is selected by Id.  A pageId querystring value takes precendence over the route (request path) for the purposes of page selection.
  /// </summary>
  public class PageRoutingMiddleware : Microsoft.AspNetCore.Http.IMiddleware
  {
    private IPageManager PageManager { get; }
    private IUserManager UserManager { get; }
    private ISiteManager SiteManager { get; }

    private IFileSystemManager FileSystemManager { get; }

    private ILogger<PageRoutingMiddleware> Logger { get; }

    private ICacheManager CacheManager { get; }

    private static readonly string[] KNOWN_NON_PAGE_PATHS =
    [
      Nucleus.Abstractions.RoutingConstants.API_ROUTE_PATH_PREFIX,
      Nucleus.Abstractions.RoutingConstants.EXTENSIONS_ROUTE_PATH_PREFIX,
      Nucleus.Abstractions.RoutingConstants.SITEMAP_ROUTE_PATH,
      Nucleus.Abstractions.RoutingConstants.FILES_ROUTE_PATH_PREFIX
    ];

    public PageRoutingMiddleware(IPageManager pageManager, ISiteManager siteManager, IUserManager userManager, IFileSystemManager fileSystemManager, ICacheManager cacheManager, ILogger<PageRoutingMiddleware> logger)
    {
      this.PageManager = pageManager;
      this.SiteManager = siteManager;
      this.UserManager = userManager;
      this.FileSystemManager = fileSystemManager;
      this.CacheManager = cacheManager;
      this.Logger = logger;
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
      Context nucleusContext = context.RequestServices.GetService<Context>();
      Application nucleusApplication = context.RequestServices.GetService<Application>();

      if (Guid.TryParse(context.Request.Query["pageid"], out Guid pageId))
      {
        Logger.LogTrace("Request for page id '{pageid}'.", pageId);
        nucleusContext.Page = await this.PageManager.Get(pageId);

        if (nucleusContext.Page != null)
        {
          if (nucleusContext.Page.Disabled)
          {
            Logger.LogTrace("Page id '{pageid}' is disabled.", pageId);
            nucleusContext.Page = null;
          }
          else
          {
            Logger.LogTrace("Page id '{pageid}' found.", pageId);
            nucleusContext.Site = await this.SiteManager.Get(nucleusContext.Page);
          }

          // When HandleLinkType returns false, it means we should not continue because we are redirecting to another page
          if (!await HandleLinkType(context, nucleusContext))
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
        if (SkipSiteDetection(context) || !nucleusApplication.IsInstalled)
        {
          Logger.LogTrace("Skipped site detection for '{request}'.", context.Request.Path);

          nucleusContext.Site = null;
        }
        else
        {
          Logger.LogTrace("Matching site by host '{host}' and pathbase '{pathbase}'.", context.Request.Host, context.Request.PathBase);

          nucleusContext.Site = await this.SiteManager.Get(context.Request.Host.Value, context.Request.PathBase);

          if (nucleusContext.Site == null)
          {
            Logger.LogTrace("Using default site.");
            nucleusContext.Site = await this.SiteManager.Get("", "");

            if (nucleusContext.Site != null)
            {
              // Add "default" site to the site alias table 
              SiteAlias alias = new() { Alias = $"{context.Request.Host}{context.Request.PathBase}" };
              await this.SiteManager.SaveAlias(nucleusContext.Site, alias);

              // If the site doesn't already have a default alias, set it to the new alias
              if (nucleusContext.Site.DefaultSiteAlias == null)
              {
                nucleusContext.Site.DefaultSiteAlias = alias;
                await this.SiteManager.Save(nucleusContext.Site);
              }
            }
          }

          if (nucleusContext.Site != null)
          {
            string requestedPath = System.Web.HttpUtility.UrlDecode(context.Request.Path);
            Logger.LogTrace("Using site '{siteid}'.", nucleusContext.Site.Id);

            if (!SkipPageDetection(context))
            {
              Logger.LogTrace("Lookup page by path '{path}'.", requestedPath);

              await FindPage(requestedPath, nucleusContext);

              if (nucleusContext.Page == null)
              {
                // if the page was not found, try searching for path & query.  This is to support pages routes which include a query string, which
                // users may have in place to provide for urls from legacy systems like DNN which can have querystring values which identify a page.
                string requestedPathAndQuery = System.Web.HttpUtility.UrlDecode(context.Request.Path + context.Request.QueryString);

                Logger.LogTrace("Lookup page by path '{path}'.", requestedPathAndQuery);

                await FindPage(requestedPathAndQuery, nucleusContext);
              }
            }

            if (nucleusContext.Page != null)
            {
              if (nucleusContext.Page.Disabled)
              {
                Logger.LogTrace("Page id '{pageid}' is disabled.", pageId);
                nucleusContext.Page = null;
              }
              else
              {
                Logger.LogTrace("Page found: '{pageid}'.", nucleusContext.Page.Id);
              }

              // When HandleLinkType returns false, it means we should not continue because we are redirecting to another site
              if (!await HandleLinkType(context, nucleusContext))
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
      }

      await next(context);

      // If the request path did not match a site, and the response is a 404 (so it didn't match a controller route
      // or any other component that can handle the request), redirect to the setup wizard, if Nucleus has not been set up.
      //
      // The criteria for checking whether Nucleus has not been set up is:
      // - The setup/install-log.config file does not exist, or the Extensions folder does not exist (Application.IsInstalled=false),
      //   or there are no sites in the database.
      // - AND there are no system administrator users in the database
      //
      // The calls to SkipSiteDetection and SkipPageDetection check that the request path isn't a known non-page path
      // like favicon.ico, or the setup wizard itself, to prevent redirection for those cases.
      if (
        context.Response.StatusCode == (int)System.Net.HttpStatusCode.NotFound &&
        nucleusContext.Site == null &&
        !SkipSiteDetection(context) &&
        !SkipPageDetection(context) &&
        (!nucleusApplication.IsInstalled || await this.SiteManager.Count() == 0) &&
        await this.UserManager.CountSystemAdministrators() == 0
      )
      {
        String relativePath = $"{(String.IsNullOrEmpty(context.Request.PathBase) ? "" : context.Request.PathBase + "/")}Setup/SiteWizard";
        context.Response.Redirect(relativePath);
      }
    }

    /// <summary>
    /// Pages can be set up as a link to an Url, file, or other page.  This function handles page link types and returns true if the page 
    /// links to something else (and thus we should skip any further processing).
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private async Task<Boolean> HandleLinkType(HttpContext context, Context nucleusContext)
    {
      switch (nucleusContext.Page.LinkType)
      {
        case Page.LinkTypes.Normal:
          return true;

        case Page.LinkTypes.Url:
          if (!String.IsNullOrEmpty(nucleusContext.Page.LinkUrl))
          {
            Logger.LogTrace("Page: '{pageid}' has link type: url.  Redirecting to '{linkUrl}'", nucleusContext.Page.Id, nucleusContext.Page.LinkUrl);
            context.Response.Redirect(nucleusContext.Page.LinkUrl);
            return false;
          }
          else
          {
            Logger.LogTrace("Page: '{pageid}' has link type: Url, but the does not have a LinkPageUrl set.  Treating as a normal page.", nucleusContext.Page.Id);
            break;
          }

        case Page.LinkTypes.Page:
          if (nucleusContext.Page.LinkPageId.HasValue)
          {
            nucleusContext.Page = await this.PageManager.Get(nucleusContext.Page.LinkPageId.Value);
            Logger.LogTrace("Page: '{pageid}' has link type: Page.  Using page id '{linkPageId}'", nucleusContext.Page.Id, nucleusContext.Page.LinkPageId);
          }
          else
          {
            Logger.LogTrace("Page: '{pageid}' has link type: Page, but the does not have a LinkPageId set.  Treating as a normal page.", nucleusContext.Page.Id);
          }
          break;

        case Page.LinkTypes.File:
          if (nucleusContext.Page.LinkFileId.HasValue)
          {
            Nucleus.Abstractions.Models.FileSystem.File file = await this.FileSystemManager.GetFile(nucleusContext.Site, nucleusContext.Page.LinkFileId.Value);
            context.Response.Redirect($"{(String.IsNullOrEmpty(context.Request.PathBase) ? "" : context.Request.PathBase + "/")}/{Nucleus.Abstractions.RoutingConstants.FILES_ROUTE_PATH_PREFIX}/{file.EncodeFileId()}");
            Logger.LogTrace("Page: '{pageid}' has link type: File.  Using file id '{linkFileId}'", nucleusContext.Page.Id, nucleusContext.Page.LinkFileId);
            return false;
          }
          else
          {
            Logger.LogTrace("Page: '{pageid}' has link type: File, but the does not have a LinkFileId set.  Treating as a normal page.", nucleusContext.Page.Id);
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
    private async Task FindPage(string requestPath, Context nucleusContext)
    {
      string pagePathCacheKey = (nucleusContext.Site.Id.ToString() + "^" + requestPath).ToLower();

      FoundPage found = await this.CacheManager.PageRouteCache().GetAsync(pagePathCacheKey, async pagePathCacheKey =>
      {
        Page page = await this.PageManager.Get(nucleusContext.Site, requestPath);

        if (page != null)
        {
          return new() { Page = page, RequestPath = requestPath, MatchedRoute = GetFoundRoute(page, requestPath) };
        }

        string partPath = requestPath;
        string parameters = "";

        while (nucleusContext.Page == null && !String.IsNullOrEmpty(partPath))
        {
          int lastIndexOfSeparator = partPath.LastIndexOfAny(['/', '&', '?']);
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

          partPath = partPath[..lastIndexOfSeparator];

          if (!String.IsNullOrEmpty(partPath))
          {
            page = await this.PageManager.Get(nucleusContext.Site, partPath);
            if (page != null)
            {
              return new() { Page = page, LocalPath = new(parameters), RequestPath = requestPath, MatchedRoute = GetFoundRoute(page, partPath) };
            }
          }
        }

        return null;
      });

      if (found != null)
      {
        nucleusContext.Page = found.Page;
        nucleusContext.LocalPath = found.LocalPath;
        nucleusContext.MatchedRoute = found.MatchedRoute;
      }
    }

    private static PageRoute GetFoundRoute(Page page, string matchedPath)
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
    private static Boolean SkipSiteDetection(HttpContext context)
    {
      // Browsers often send a request for /favicon.ico even when the page doesn't specify an icon.  When a site is set up with an "favicon", the
      // path is a link to /files/path, not /favicon.ico, so /favicon.ico is never a legitimate request.
      if (context.Request.Path.Value.Equals("/favicon.ico"))
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

      return false;
    }

    /// <summary>
    /// Gets whether to skip page detection
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// Skip logic to identify the current page when the http request is for a file, API method, extension resource file or the search engine site map.
    /// </remarks>
    private static Boolean SkipPageDetection(HttpContext context)
    {
      if (context.Request.Path.HasValue && context.Request.Path != "/")
      {
        // skip page detection when the request is for a known reserved path.
        return KNOWN_NON_PAGE_PATHS.Where(knownPath => context.Request.Path.StartsWithSegments("/" + knownPath, StringComparison.OrdinalIgnoreCase)).Any();
      }

      return false;
    }
  }
}
