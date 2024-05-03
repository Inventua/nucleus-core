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
  private ISiteManager SiteManager { get; }
  private IUserManager UserManager { get; }
  private Application Application { get; }

  // Files and extensions in these lists do not show the site error page, they always return a 404 if they are not found.
  private static readonly HashSet<string> filteredFilenames = new(new string[] { "favicon.ico", "robots.txt" }, StringComparer.OrdinalIgnoreCase);
  private static readonly HashSet<string> filteredFileExtensions = new(new string[] { ".txt", ".css", ".js", ".map" }, StringComparer.OrdinalIgnoreCase);

  public DefaultController(IWebHostEnvironment webHostEnvironment, ILogger<DefaultController> logger, Context context, Application application, ISiteManager siteManager, IUserManager userManager, IFileSystemManager fileSystemManager, IPageManager pageManager)
  {
    this.WebHostEnvironment = webHostEnvironment;
    this.Application = application;
    this.Logger = logger;
    this.Context = context;
    this.FileSystemManager = fileSystemManager;
    this.PageManager = pageManager;
    this.SiteManager = siteManager;
    this.UserManager = userManager;
  }

  [HttpGet]
  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.PAGE_VIEW_POLICY)]
  public async Task<ActionResult> Index()
  {
    Boolean useSiteErrorPage = false;

    // If the site hasn't been set up yet (empty database), redirect to the site wizard.
    if (this.Context.Site == null && this.Context.Page == null)
    {
      if (this.ShouldRedirectToSetupWizard())
      {
        return RedirectToAction("Index", "SiteWizard", new { area = "Setup" });
      }
    }

    // if the user's password has expired, redirect them to the change password page, as long as the current request isn't for the change password page
    if (User.IsPasswordExpired())
    {
      SitePages sitePages = this.Context.Site.GetSitePages();
      Page loginPage = null;

      if (sitePages.UserChangePasswordPageId.HasValue)
      {
        loginPage = await this.PageManager.Get(sitePages.UserChangePasswordPageId.Value);
      }

      if (loginPage != null)
      {
        if (this.Context.Page.Id != loginPage.Id)
        {
          string redirectUrl = loginPage.DefaultPageRoute()?.Path;

          if (!String.IsNullOrEmpty(redirectUrl))
          {
            Logger.LogTrace("Redirecting user with expired password to {redirectUrl}.", redirectUrl);
            return Redirect(redirectUrl);
          }
          else
          {
            Logger.LogWarning("Unable to redirect a user with an expired password to the 'Change Password' page because the site's configured 'Change Password' page does not have a default route.", redirectUrl);
          }
        }
      }
      else
      {
        // if the site does not have a "change password" page set, redirect to the built-in one instead

        return Redirect(Url.AreaAction("EditPassword", "User", "Account", new { returnUrl = this.HttpContext.Request.ToString() }));
      }
    }

    // If the page was not found, display the error page if one is defined for the site, or if the requested Uri is one
    // of the file names/extensions that we always return a "raw" 404 error for, return NotFound().
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

      // Either no error page is defined, or the requested url matches one of the file names/extensions that we always return 
      // a "raw" 404 error for
      if (!useSiteErrorPage)
      {
        return NotFound();
      }
    }

    if (!useSiteErrorPage)
    {
      // Handle "PermanentRedirect" page routes
      foreach (PageRoute pageRoute in this.Context.Page.Routes.ToArray())
      {
        if (this.Context.MatchedRoute?.Type == PageRoute.PageRouteTypes.PermanentRedirect)
        {
          string redirectUrl = this.Url.PageLink(this.Context.Page);
          Logger.LogTrace("Permanently redirecting request to {redirectUrl}.", redirectUrl);
          return RedirectPermanent(redirectUrl);
        }
      }
    }

    ////Nucleus.ViewFeatures.ViewModels.Layout viewModel = new(this.Context);

    ////viewModel.ControlPanelUri = this.Application.ControlPanelUri;
    ////viewModel.IsEditing = User.IsEditing(HttpContext, this.Context.Site, this.Context.Page);
    ////viewModel.CanEdit = User.CanEditContent(this.Context.Site, this.Context.Page) && viewModel.ControlPanelUri != "";
    ////viewModel.DefaultPageUri = base.Url.GetAbsoluteUri(this.Context.Page.DefaultPageRoute().Path).AbsoluteUri;
    ////viewModel.SiteIconPath = Url.Content(await Context.Site.GetIconPath(this.FileSystemManager));
    ////viewModel.SiteCssFilePath = Url.Content(await Context.Site.GetCssFilePath(this.FileSystemManager));
    ////viewModel.ControlPanelDockingCssClass = viewModel.CanEdit && IsTopDockSelected(ControllerContext.HttpContext) ? "control-panel-dock-top" : "";


    ////if (viewModel.IsEditing)
    ////{
    ////  // refresh editing cookie expiry
    ////  Microsoft.AspNetCore.Http.CookieOptions options = new()
    ////  {
    ////    Expires = DateTime.UtcNow.AddMinutes(60),
    ////    IsEssential = true,
    ////    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict
    ////  };

    ////  ControllerContext.HttpContext.Response.Cookies.Append(PermissionExtensions.EDIT_COOKIE_NAME, "true", options);
    ////}

    ////string layoutPath = this.Context.Page.LayoutPath(this.Context.Site);

    ////if (!System.IO.File.Exists(System.IO.Path.Join(this.WebHostEnvironment.ContentRootPath, layoutPath)))
    ////{
    ////  layoutPath = $"{Nucleus.Abstractions.Models.Configuration.FolderOptions.LAYOUTS_FOLDER}/{Nucleus.Abstractions.Managers.ILayoutManager.DEFAULT_LAYOUT}";
    ////}

    ////return View(layoutPath, viewModel);
    return View(GetLayoutPath(this.WebHostEnvironment, this.Context), await BuildViewModel(this.Url, this.Context, this.HttpContext, this.Application, this.FileSystemManager ));
  }

  internal static string GetLayoutPath(IWebHostEnvironment env, Context context)
  {
    string layoutPath = context.Page.LayoutPath(context.Site);

    if (!System.IO.File.Exists(System.IO.Path.Join(env.ContentRootPath, layoutPath)))
    {
      layoutPath = $"{Nucleus.Abstractions.Models.Configuration.FolderOptions.LAYOUTS_FOLDER}/{Nucleus.Abstractions.Managers.ILayoutManager.DEFAULT_LAYOUT}";
    }

    return layoutPath;
  }

  internal static async Task<Nucleus.ViewFeatures.ViewModels.Layout> BuildViewModel(IUrlHelper url, Context context, HttpContext httpContext, Application app, IFileSystemManager fileSystemManager)
  {    
    Nucleus.ViewFeatures.ViewModels.Layout viewModel = new(context);

    viewModel.ControlPanelUri = app.ControlPanelUri;
    viewModel.IsEditing = httpContext.User.IsEditing(httpContext, context.Site, context.Page);
    viewModel.CanEdit = httpContext.User.CanEditContent(context.Site, context.Page) && viewModel.ControlPanelUri != "";
    viewModel.DefaultPageUri = url.GetAbsoluteUri(context.Page.DefaultPageRoute().Path).AbsoluteUri;
    viewModel.SiteIconPath = url.Content(await context.Site.GetIconPath(fileSystemManager));
    viewModel.SiteCssFilePath = url.Content(await context.Site.GetCssFilePath(fileSystemManager));
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

  private Boolean ShouldRedirectToSetupWizard()
  {
    return (!this.Application.IsInstalled);
  }

  //private async Task<Boolean> RedirectToInstallWizard()
  //{
  //  // if the wizard hasn't run AND there are no sites AND no system administrators, run the wizard
  //  if (!this.Application.IsInstalled && await this.SiteManager.Count() == 0 && await this.UserManager.CountSystemAdministrators() == 0)
  //  {
  //    return true;
  //  }

  //  // if nucleus thinks that the wizard HAS run but there are no sites AND no system administrators, run the wizard.  The logic here
  //  // is repeated from the previous case because this.Application.IsInstalled returns quickly and this function is called frequently.
  //  if (await this.SiteManager.Count() == 0 && await this.UserManager.CountSystemAdministrators() == 0)
  //  {
  //    return true;
  //  }

  //  return false;
  //}

  /// <summary>
  /// Return true to prevent redirection to the "friendly" 404 page if the form of the request path (or another condition) means
  /// that the user agent should get a "real" 404 error.
  /// </summary>
  /// <returns></returns>
  private Boolean No404Redirect()
  {
    if (filteredFilenames.Contains(System.IO.Path.GetFileName(ControllerContext.HttpContext.Request.Path)))
    {
      return true;
    }

    if (filteredFileExtensions.Contains(System.IO.Path.GetExtension(ControllerContext.HttpContext.Request.Path)))
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
