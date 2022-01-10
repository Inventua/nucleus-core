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
	[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
	public class RoleGroupsController : Controller
	{
		private ILogger<RoleGroupsController> Logger { get; }
		private RoleGroupManager RoleGroupManager { get; }
		private Context Context { get; }

		public RoleGroupsController(Context context, ILogger<RoleGroupsController> logger, RoleGroupManager roleGroupManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.RoleGroupManager = roleGroupManager;
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
			ViewModels.Admin.RoleGroupEditor viewModel;

			
			viewModel = BuildViewModel(id == Guid.Empty ? this.RoleGroupManager.CreateNew() : this.RoleGroupManager.Get(id));
			

			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult AddRoleGroup()
		{
			return View("Editor", BuildViewModel(new RoleGroup()));
		}


		[HttpPost]
		public ActionResult Save(ViewModels.Admin.RoleGroupEditor viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			this.RoleGroupManager.Save(this.Context.Site, viewModel.RoleGroup);		

			return View("Index", BuildViewModel());
		}

		[HttpPost]
		public ActionResult DeleteRoleGroup(ViewModels.Admin.RoleGroupEditor viewModel)
		{
			this.RoleGroupManager.Delete(viewModel.RoleGroup);
			return View("Index", BuildViewModel());
		}

		private ViewModels.Admin.RoleGroupIndex BuildViewModel()
		{
			ViewModels.Admin.RoleGroupIndex viewModel = new();
						
			viewModel.RoleGroups = this.RoleGroupManager.List(this.Context.Site);
			
			return viewModel;
		}

		private ViewModels.Admin.RoleGroupEditor BuildViewModel(RoleGroup roleGroup)
		{
			ViewModels.Admin.RoleGroupEditor viewModel = new();

			viewModel.RoleGroup = roleGroup;
			
			return viewModel;
		}
	}
}
