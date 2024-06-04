using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions.Logging;

namespace Nucleus.Core.Layout;

/// <summary>
/// Provides support for API (/api/modules/[module]/[action]) routes where the request path contains a "mid" querystring value by finding the 
/// corresponding module and populating the context object's page and module properties.  The context object is a singleton DI object.
/// </summary>
public class ModuleRoutingMiddleware : Microsoft.AspNetCore.Http.IMiddleware
{
  private IPageManager PageManager { get; }
  private IPageModuleManager PageModuleManager { get; }
  private ISiteManager SiteManager { get; }
  private ILogger<ModuleRoutingMiddleware> Logger { get; }

  public ModuleRoutingMiddleware(ILogger<ModuleRoutingMiddleware> logger, IPageManager pageManager, IPageModuleManager pageModuleManager, ISiteManager siteManager)
  {
    this.Logger = logger;
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
    PageModule moduleInfo;
    Site site = null;
    Page page = null;
    string mid = null;
    Context nucleusContext = context.RequestServices.GetService<Context>();

    if (context.Request.RouteValues.ContainsKey("mid"))
    {
      mid = context.Request.RouteValues["mid"].ToString();
    }
    else if (context.Request.Query.ContainsKey("mid"))
    {
      mid = context.Request.Query["mid"];
    }

    if (mid != null && Guid.TryParse(mid, out Guid moduleId))
    {
      Logger.LogTrace("Request for module id {0}", moduleId);

      moduleInfo = await this.PageModuleManager.Get(moduleId);

      if (moduleInfo == null)
      {
        Logger.LogTrace("Module id {0} not found.", moduleId);
      }
      else
      {
        page = await this.PageManager.Get(moduleInfo);
        if (page == null)
        {
          Logger.LogTrace("Page for module '{0}' not found.", moduleInfo.Id);
        }
        else
        {
          site = await this.SiteManager.Get(page);
          if (site == null)
          {
            Logger.LogTrace("Site for page '{0}' not found.", page.Id);
          }
          else
          {
            if (context.User.HasViewPermission(site, page, moduleInfo) || context.User.HasEditPermission(site, page, moduleInfo))
            {
              Logger.LogTrace("Module id '{0}' found.", moduleId);

              nucleusContext.Module = moduleInfo;
              nucleusContext.Page = page;
              nucleusContext.Site = site;
            }
            else
            {
              Logger.LogTrace("Permission denied for module id '{0}', user '{1}'.", moduleId, context.User.GetUserId());
              context.Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
              return;
            }
          }
        }
      }
    }

    await next(context);
  }
}
