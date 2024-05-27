using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions.Logging;
using Nucleus.ViewFeatures;

namespace Nucleus.Web.Controllers;

/// <summary>
/// Display a page and module content, using the selected layout
/// </summary>
public class DefaultController : Controller
{
  private IWebHostEnvironment WebHostEnvironment { get; }
  private ILogger<DefaultController> Logger { get; }
  private Context Context { get; }
  private IFileSystemManager FileSystemManager { get; }
  private IPageManager PageManager { get; }
  
  private Application Application { get; }

  // Files and extensions in these lists do not show the site error page, they always return a 404 if they are not found.
  private static readonly HashSet<string> FILTERED_FILE_NAMES = new(["favicon.ico", "robots.txt"], StringComparer.OrdinalIgnoreCase);
  private static readonly HashSet<string> FILTERED_FILE_EXTENSIONS = new([".txt", ".css", ".js", ".map"], StringComparer.OrdinalIgnoreCase);

  public DefaultController(IWebHostEnvironment webHostEnvironment, ILogger<DefaultController> logger, Context context, Application application, IFileSystemManager fileSystemManager, IPageManager pageManager)
  {
    this.WebHostEnvironment = webHostEnvironment;
    this.Application = application;
    this.Logger = logger;
    this.Context = context;
    this.FileSystemManager = fileSystemManager;
    this.PageManager = pageManager;
  }

  [HttpGet]
  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_VIEW_POLICY)]
  public async Task<ActionResult> Index()
  {
    Boolean useSiteErrorPage = false;

    // if the user's password has expired, redirect them to the change password page, as long as the current request isn't for the change password page
    if (User.IsPasswordExpired())
    {
      SitePages sitePages = this.Context.Site.GetSitePages();
      Page changePasswordPage = null;

      if (sitePages.UserChangePasswordPageId.HasValue)
      {
        changePasswordPage = await this.PageManager.Get(sitePages.UserChangePasswordPageId.Value);
      }

      if (changePasswordPage != null)
      {
        if (this.Context.Page.Id != changePasswordPage.Id)
        {
          string redirectUrl = changePasswordPage.DefaultPageRoute()?.Path;

          if (!String.IsNullOrEmpty(redirectUrl))
          {
            Logger.LogTrace("Redirecting user '{username}' with expired password to '{redirectUrl}'.", User.Identity.Name, redirectUrl);
            return Redirect(redirectUrl);
          }
          else
          {
            Logger.LogWarning("Unable to redirect user '{username}' with an expired password to the 'Change Password' page because the site's configured 'Change Password' page does not have a default route.", User.Identity.Name);
          }
        }
      }
      else
      {
        // if the site does not have a "change password" page set, redirect to the built-in one instead
        return Redirect(Url.AreaAction("EditPassword", "User", "Account", new { returnUrl = this.HttpContext.Request.ToString() }));
      }
    }

    // If the page was not found, display the error page if one is defined for the site, and the request does not match a
    // "known" file name or extension (checked in No404Redirect).  Otherwise, return a 404: NotFound.
    if (this.Context.Page == null)
    {
      Logger.LogTrace("Not found: {path}.", ControllerContext.HttpContext.Request.Path);

      if (!No404Redirect() && this.Context.Site != null)
      {
        SitePages sitePages = this.Context.Site.GetSitePages();
        Page notFoundPage;
        if (sitePages.NotFoundPageId.HasValue)
        {
          notFoundPage = await this.PageManager.Get(sitePages.NotFoundPageId.Value);
          if (notFoundPage != null)
          {
            this.Context.Page = notFoundPage;
            this.ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            useSiteErrorPage = true;
          }
        }
      }

      // return a "raw" 404 error 
      if (!useSiteErrorPage)
      {
        return NotFound();
      }
    }

    // Handle "PermanentRedirect" page routes.
    // We check useSiteErrorPage here because when we are showing the site's "friendly" 404/Not Found  page, we don't return a redirect, we
    // set context.Page to the 404 error page, so we end up here.  We do not need to process permanent redirects on the requested page when
    // we are displaying the site's "not found" page (there won't be any, since the page was not found).
    if (!useSiteErrorPage)
    {
      if (this.Context.MatchedRoute?.Type == PageRoute.PageRouteTypes.PermanentRedirect)
      {
        string redirectUrl = this.Url.PageLink(this.Context.Page);
        Logger.LogTrace("Permanently redirecting request '{originalRequest}' to '{redirectUrl}'.", Request.Path, redirectUrl);
        return RedirectPermanent(redirectUrl);
      }      
    }

    // display the requested page
    return View(GetLayoutPath(this.WebHostEnvironment, this.Context, this.Logger), await BuildViewModel(this.Url, this.Context, this.HttpContext, this.Application, this.FileSystemManager ));
  }

  internal static string GetLayoutPath(IWebHostEnvironment env, Context context, ILogger logger)
  {
    string layoutPath = context.Page.LayoutPath(context.Site);
    if (!env.ContentRootFileProvider.GetFileInfo(layoutPath).Exists)
    {
      logger.LogWarning("A page with title '{title}' and route '{route}' is configured to use a missing layout '{layout}'.  The default layout was used instead.", context.Page.Title, context.MatchedRoute.Path, layoutPath);
      layoutPath = $"{Nucleus.Abstractions.Models.Configuration.FolderOptions.LAYOUTS_FOLDER}/{Nucleus.Abstractions.Managers.ILayoutManager.DEFAULT_LAYOUT}";
    }

    return layoutPath;
  }

  internal static async Task<Nucleus.ViewFeatures.ViewModels.Layout> BuildViewModel(IUrlHelper url, Context context, HttpContext httpContext, Application app, IFileSystemManager fileSystemManager)
  {
    Nucleus.ViewFeatures.ViewModels.Layout viewModel = new(context)
    {
      CanEdit = httpContext.User.CanEditContent(context.Site, context.Page) && app.ControlPanelUri != "",
      ControlPanelUri = app.ControlPanelUri,
      IsEditing = httpContext.User.IsEditing(httpContext, context.Site, context.Page),
      DefaultPageUri = url.GetAbsoluteUri(context.Page.DefaultPageRoute().Path).AbsoluteUri,
      SiteIconPath = url.Content(await context.Site.GetIconPath(fileSystemManager)),
      SiteCssFilePath = url.Content(await context.Site.GetCssFilePath(fileSystemManager))
    };

    viewModel.ControlPanelDockingCssClass = viewModel.CanEdit && IsTopDockSelected(httpContext) ? "control-panel-dock-top" : "";   

    if (viewModel.IsEditing)
    {
      // refresh editing cookie expiry
      Microsoft.AspNetCore.Http.CookieOptions options = new()
      {
        Expires = DateTime.UtcNow.AddMinutes(60),
        IsEssential = true,
        SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict
      };

      httpContext.Response.Cookies.Append(PermissionExtensions.EDIT_COOKIE_NAME, "true", options);
    }

    return viewModel;
  }

  /// <summary>
  /// Return true to prevent redirection to the "friendly" 404 page if the form of the request path (or another condition) means
  /// that the user agent should get a "real" 404 error.
  /// </summary>
  /// <returns></returns>
  private Boolean No404Redirect()
  {
    if (FILTERED_FILE_NAMES.Contains(System.IO.Path.GetFileName(ControllerContext.HttpContext.Request.Path)))
    {
      return true;
    }

    if (FILTERED_FILE_EXTENSIONS.Contains(System.IO.Path.GetExtension(ControllerContext.HttpContext.Request.Path)))
    {
      return true;
    }

    return false;
  }

  private static Boolean IsTopDockSelected(Microsoft.AspNetCore.Http.HttpContext context)
  {
    if (Boolean.TryParse(context.Request.Cookies[PermissionExtensions.CONTROL_PANEL_DOCKING_COOKIE_NAME], out Boolean isSelected))
    {
      return isSelected;
    }

    return false;
  }
}
