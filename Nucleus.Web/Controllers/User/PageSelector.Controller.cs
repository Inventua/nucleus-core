using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using System.Threading.Tasks;

namespace Nucleus.Web.Controllers
{
	[Area(AREA_NAME)]
	public class PageSelectorController : Controller
	{
		public const string AREA_NAME = "User";

		private Context Context { get; }
		private IPageManager PageManager { get; }

		public PageSelectorController(Context context, IPageManager pageManager)
		{
			this.Context = context;
			this.PageManager = pageManager;			
		}

		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Index(ViewModels.User.PageSelector viewModel)
		{
			if (viewModel.PageMenu == null)
			{
				viewModel.PageMenu = await this.PageManager.GetAdminMenu(this.Context.Site, null, this.ControllerContext.HttpContext.User, 1);
			}
			return View("Index", viewModel);
		}

		[HttpGet]
		public async Task<ActionResult> GetChildPages(Guid id)
		{
			ViewModels.User.PageSelector viewModel = new();

			viewModel.FromPage = await this.PageManager.Get(id);

			viewModel.PageMenu = await this.PageManager.GetAdminMenu
				(
					this.Context.Site,
					await this.PageManager.Get(id),
					ControllerContext.HttpContext.User,
					1
				);

			return View("PageMenu", viewModel);
		}
	}
}

