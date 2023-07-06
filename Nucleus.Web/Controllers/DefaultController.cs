using Microsoft.AspNetCore.Mvc;
using System;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Mvc.Routing;
using Nucleus.ViewFeatures;
using Microsoft.Extensions.Options;
using Nucleus.Extensions.Authorization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Nucleus.Web.Controllers
{
	/// <summary>
	/// Display a page and module content, using the selected layout
	/// </summary>
	public class DefaultController : Controller
	{
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

		public DefaultController(ILogger<DefaultController> logger, Context context, Application application, ISiteManager siteManager, IUserManager userManager, IFileSystemManager fileSystemManager, IPageManager pageManager)
		{
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
				if (this.RedirectToSetupWizard())
				{					
					return RedirectToAction("Index", "SiteWizard", new { area = "Setup" });
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
					if (pageRoute.Path.Equals(ControllerContext.HttpContext.Request.Path, StringComparison.OrdinalIgnoreCase) || pageRoute.Path.Equals(ControllerContext.HttpContext.Request.Path + ControllerContext.HttpContext.Request.QueryString, StringComparison.OrdinalIgnoreCase))
					{
						if (pageRoute.Type == PageRoute.PageRouteTypes.PermanentRedirect)
						{
							string redirectUrl = this.Url.PageLink(this.Context.Page);
							Logger.LogTrace("Permanently redirecting request to {redirectUrl}.", redirectUrl);
							return RedirectPermanent(redirectUrl);
						}
					}
				}				
			}

			Nucleus.ViewFeatures.ViewModels.Layout viewModel = new(this.Context);

			viewModel.IsEditing = User.IsEditing(HttpContext, this.Context.Site, this.Context.Page);
			viewModel.CanEdit = User.CanEditContent(this.Context.Site, this.Context.Page);
			viewModel.DefaultPageUri = base.Url.GetAbsoluteUri(this.Context.Page.DefaultPageRoute().Path).AbsoluteUri;
			viewModel.SiteIconPath = Url.Content(await Context.Site.GetIconPath(this.FileSystemManager));
			viewModel.SiteCssFilePath = Url.Content(await Context.Site.GetCssFilePath(this.FileSystemManager));

			if (viewModel.IsEditing)
			{				
				// refresh editing cookie expiry
				Microsoft.AspNetCore.Http.CookieOptions options = new()
				{
					Expires = DateTime.UtcNow.AddMinutes(60),
					IsEssential = true,
					SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict
				};

				ControllerContext.HttpContext.Response.Cookies.Append(PermissionExtensions.EDIT_COOKIE_NAME, "true", options);
			}

			return View(this.Context.Page.LayoutPath(this.Context.Site), viewModel);
		}

		private Boolean RedirectToSetupWizard()
		{
			return (!this.Application.IsInstalled);
		}

		private async Task<Boolean> RedirectToInstallWizard()
    {
			// if the wizard hasn't run AND there are no sites AND no system administrators, run the wizard
			if (!this.Application.IsInstalled && await this.SiteManager.Count() == 0 && await this.UserManager.CountSystemAdministrators() == 0)
			{
				return true;
      }

			// if nucleus thinks that the wizard HAS run but there are no sites AND no system administrators, run the wizard.  The logic here
			// is repeated from the previous case because this.Application.IsInstalled returns quickly and this function is called frequently.
			if (await this.SiteManager.Count() == 0 && await this.UserManager.CountSystemAdministrators() == 0)
      {
				return true;
      }

			return false;
		}

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
	}
}
