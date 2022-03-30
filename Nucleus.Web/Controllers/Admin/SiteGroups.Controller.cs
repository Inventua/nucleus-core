using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
	public class SiteGroupsController : Controller
	{
		private ILogger<SiteGroupsController> Logger { get; }
		private ISiteGroupManager SiteGroupManager { get; }
		private ISiteManager SiteManager { get; }

		private Context Context { get; }

		public SiteGroupsController(Context context, ILogger<SiteGroupsController> logger, ISiteGroupManager siteGroupManager, ISiteManager siteManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.SiteGroupManager = siteGroupManager;
			this.SiteManager = siteManager;
		}

		/// <summary>
		/// Display the site group editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Index()
		{
			return View("Index", await BuildViewModel());
		}

		/// <summary>
		/// Display the site group list
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public async Task<ActionResult> List(ViewModels.Admin.SiteGroupIndex viewModel)
		{
			return View("_SiteGroupsList", await BuildViewModel(viewModel));
		}


		/// <summary>
		/// Display the user editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Editor(Guid id)
		{
			ViewModels.Admin.SiteGroupEditor viewModel;
			
			viewModel = await BuildViewModel(id == Guid.Empty ? await this.SiteGroupManager.CreateNew() : await this.SiteGroupManager.Get(id));
			
			return View("Editor", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> AddSiteGroup()
		{
			return View("Editor", await BuildViewModel(new SiteGroup()));
		}

		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Admin.SiteGroupEditor viewModel)
		{
			ControllerContext.ModelState.Remove("SiteGroup.PrimarySite.Name");
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			try
			{
				await this.SiteGroupManager.Save(viewModel.SiteGroup);
			}
			catch (Exception e)
			{
				if (e.Message.Contains("UNIQUE constraint failed: SiteGroups.PrimarySiteId", StringComparison.OrdinalIgnoreCase))
				{
					return Conflict("The primary site that you have selected is already in use by another site group.");
				}
				else
				{
					throw;
				}
			}
			return View("Index", await BuildViewModel());
		}

		[HttpPost]
		public async Task<ActionResult> DeleteSiteGroup(ViewModels.Admin.SiteGroupEditor viewModel)
		{
			await this.SiteGroupManager.Delete(viewModel.SiteGroup);
			return View("Index", await BuildViewModel());
		}

		private async Task<ViewModels.Admin.SiteGroupIndex> BuildViewModel()
		{
			return await BuildViewModel(new ViewModels.Admin.SiteGroupIndex());
		}

		private async Task<ViewModels.Admin.SiteGroupIndex> BuildViewModel(ViewModels.Admin.SiteGroupIndex viewModel)
		{						
			viewModel.SiteGroups = await this.SiteGroupManager.List(viewModel.SiteGroups);
			
			return viewModel;
		}

		private async Task<ViewModels.Admin.SiteGroupEditor> BuildViewModel(SiteGroup siteGroup)
		{
			ViewModels.Admin.SiteGroupEditor viewModel = new();

			viewModel.SiteGroup = siteGroup;
			viewModel.Sites = await this.SiteManager.List();

			return viewModel;
		}
	}
}
