using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	public class IndexController : Controller
	{
		ILogger<IndexController> Logger { get; }

		/// <summary>
		/// Current Nucleus context.
		/// </summary>
		/// <remarks>
		/// For requests to the admin components, the context will only ever contain a Site, the Page and Module properties will be null.
		/// </remarks>
		private Context Context { get; set; }
		private IPageManager PageManager { get; }

		public IndexController(ILogger<IndexController> Logger, Context context, IPageManager pageManager)
		{
			this.Logger = Logger;
			this.Context = context;
			this.PageManager = pageManager;
		}

		/// <summary>
		/// Display the admin controls "minimized" menu
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Index(Guid pageId)
		{
			ViewModels.Admin.Index viewModel = new ViewModels.Admin.Index();
			viewModel.CurrentPage = this.Context.Page;
			viewModel.IsSystemAdministrator = ControllerContext.HttpContext.User.IsSystemAdministrator();
			viewModel.IsSiteAdmin = ControllerContext.HttpContext.User.IsSiteAdmin(Context.Site);

			// For requests to the admin components, the context will only ever contain a Site, the Page and Module properties will be null.
			// We pass the page Id in to the admin request as a query string element.
			Page page = await this.PageManager.Get(pageId);

			viewModel.CanEdit = ControllerContext.HttpContext.User.CanEditContent(this.Context.Site, page);
			viewModel.IsEditMode = (viewModel.CanEdit && ControllerContext.HttpContext.User.IsEditing(ControllerContext.HttpContext));

			return View("Index", viewModel);
		}

	}
}