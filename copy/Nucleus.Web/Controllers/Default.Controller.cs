using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Core.Authorization;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Nucleus.ViewFeatures;
using Nucleus.Core;

namespace Nucleus.Web.Controllers
{
	/// <summary>
	/// Display a page and module content, using the selected layout
	/// </summary>
	public class DefaultController : Controller
	{
		private ILogger<DefaultController> Logger { get; }
		private Context Context { get; }
		private FileSystemManager FileSystemManager { get; }

		public DefaultController(ILogger<DefaultController> logger, Context context, FileSystemManager fileSystemManager)
		{
			this.Logger = logger;
			this.Context = context;
			this.FileSystemManager = fileSystemManager;
		}

		[HttpGet]
		[Authorize(Policy = Nucleus.Core.Authorization.PageViewPermissionAuthorizationHandler.PAGE_VIEW_POLICY)]
		public ActionResult Index()
		{
			string layoutPath;

			if (this.Context.Page == null)
			{
				return NotFound();
			}

			// Handle "PermanentRedirect" page routes
			foreach (PageRoute pageRoute in this.Context.Page.Routes.ToArray())
			{
				if (pageRoute.Path.Equals(ControllerContext.HttpContext.Request.Path, StringComparison.OrdinalIgnoreCase))
				{
					if (pageRoute.Type == PageRoute.PageRouteTypes.PermanentRedirect)
					{
						PageRoute target = this.Context.Page.Routes.Where(route => route.Id == this.Context.Page.DefaultPageRouteId).FirstOrDefault();

						if (target != null && target.Id != pageRoute.Id)
						{
							return RedirectPermanent(target.Path);
						}
					}
				}
			}

			DateTime pageLastModifiedDate = Context.Page.DateChanged == DateTime.MinValue ? Context.Page.DateAdded : Context.Page.DateChanged;
			foreach (PageModule module in Context.Page.Modules)
			{
				DateTime moduleLastChanged = module.DateChanged == DateTime.MinValue ? module.DateAdded : module.DateChanged;
				if (moduleLastChanged > pageLastModifiedDate)
				{
					pageLastModifiedDate = moduleLastChanged;
				}
			}

			DateTimeOffset? ifModifiedSince = ControllerContext.HttpContext.Request.GetTypedHeaders().IfModifiedSince;
			if (ifModifiedSince.HasValue && ifModifiedSince.Value >= pageLastModifiedDate)
			{				
				return StatusCode((int)System.Net.HttpStatusCode.NotModified);				
			}

			if (this.Context.Page.Layout == null)
			{
				if (this.Context.Site.DefaultLayout == null)
				{
					layoutPath = $"{Folders.LAYOUTS_FOLDER}\\{Nucleus.Core.LayoutManager.DEFAULT_LAYOUT}";
				}
				else
				{
					layoutPath = this.Context.Site.DefaultLayout.RelativePath;
				}
			}
			else
			{
				layoutPath = this.Context.Page.Layout.RelativePath;
			}

			ViewModels.Default viewModel = new ViewModels.Default(this.Context);
			viewModel.IsEditing = User.IsEditing(HttpContext, this.Context.Site, this.Context.Page);

			viewModel.CanEdit = User.CanEditContent(this.Context.Site, this.Context.Page);
			viewModel.DefaultPageUri = base.Url.GetAbsoluteUri(this.Context.Page.DefaultPageRoute().Path).AbsoluteUri;

			
			if (Guid.TryParse(Context.Site.SiteSettings.TryGetValue(Site.SiteImageKeys.FAVICON_FILEID), out Guid fileId))
			{
				Nucleus.Abstractions.Models.FileSystem.File iconFile =this.FileSystemManager.GetFile(this.Context.Site, fileId);
				viewModel.SiteIconPath = base.Url.DownloadLink(iconFile, false);
			}

			//string iconFileProvider = Context.Site.SiteSettings.TryGetValue(Site.SiteFavIconKeys.FAVICON_PROVIDER);
			//string iconFilePath = Context.Site.SiteSettings.TryGetValue(Site.SiteFavIconKeys.FAVICON_PATH);
			//if (!String.IsNullOrEmpty(iconFileProvider) && !String.IsNullOrEmpty(iconFilePath))
			//{
			//	viewModel.SiteIconPath = base.Url.DownloadLink(iconFileProvider, iconFilePath, false);
			//}
			
			return View(layoutPath, viewModel);
		}

	}
}
