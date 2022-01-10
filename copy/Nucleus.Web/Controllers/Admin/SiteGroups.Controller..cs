using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Core;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Core.Authorization.SystemAdminAuthorizationHandler.SYSTEM_ADMIN_POLICY)]
	public class SiteGroupsController : Controller
	{
		private ILogger<SiteGroupsController> Logger { get; }
		private SiteGroupManager SiteGroupManager { get; }
		private SiteManager SiteManager { get; }

		private Context Context { get; }

		public SiteGroupsController(Context context, ILogger<SiteGroupsController> logger, SiteGroupManager siteGroupManager, SiteManager siteManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.SiteGroupManager = siteGroupManager;
			this.SiteManager = siteManager;
		}

		/// <summary>
		/// Display the page editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public ActionResult Index()
		{
			return View("Index", BuildViewModel());
		}

		/// <summary>
		/// Display the user editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public ActionResult Editor(Guid id)
		{
			ViewModels.Admin.SiteGroupEditor viewModel;

			
			viewModel = BuildViewModel(id == Guid.Empty ? this.SiteGroupManager.CreateNew() : this.SiteGroupManager.Get(id));
			

			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult AddSiteGroup()
		{
			return View("Editor", BuildViewModel(new SiteGroup()));
		}


		[HttpPost]
		public ActionResult Save(ViewModels.Admin.SiteGroupEditor viewModel)
		{
			ControllerContext.ModelState.Remove("SiteGroup.PrimarySite.Name");
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			try
			{
				this.SiteGroupManager.Save(viewModel.SiteGroup);
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
			return View("Index", BuildViewModel());
		}

		[HttpPost]
		public ActionResult DeleteSiteGroup(ViewModels.Admin.SiteGroupEditor viewModel)
		{
			this.SiteGroupManager.Delete(viewModel.SiteGroup);
			return View("Index", BuildViewModel());
		}

		private ViewModels.Admin.SiteGroupIndex BuildViewModel()
		{
			ViewModels.Admin.SiteGroupIndex viewModel = new();
						
			viewModel.SiteGroups = this.SiteGroupManager.List();
			
			return viewModel;
		}

		private ViewModels.Admin.SiteGroupEditor BuildViewModel(SiteGroup siteGroup)
		{
			ViewModels.Admin.SiteGroupEditor viewModel = new();

			viewModel.SiteGroup = siteGroup;
			viewModel.Sites = this.SiteManager.List();

			return viewModel;
		}
	}
}
