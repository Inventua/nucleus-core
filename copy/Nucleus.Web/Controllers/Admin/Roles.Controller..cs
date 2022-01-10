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
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Core;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
	public class RolesController : Controller
	{
		private ILogger<RolesController> Logger { get; }
		private RoleManager RoleManager { get; }
		private RoleGroupManager RoleGroupManager { get; }
		private Context Context { get; }

		public RolesController(Context context, ILogger<RolesController> logger, RoleManager roleManager, RoleGroupManager roleGroupManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.RoleManager = roleManager;
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
			ViewModels.Admin.RoleEditor viewModel;

			viewModel = BuildViewModel(id == Guid.Empty ? RoleManager.CreateNew() : RoleManager.Get(id));
			
			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult AddRole()
		{
			return View("Editor", BuildViewModel(new Role()));
		}

		[HttpPost]
		public ActionResult Save(ViewModels.Admin.RoleEditor viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			this.RoleManager.Save(this.Context.Site, viewModel.Role);
			
			return View("Index", BuildViewModel());
		}

		[HttpPost]
		public ActionResult DeleteRole(ViewModels.Admin.RoleEditor viewModel)
		{
			this.RoleManager.Delete(viewModel.Role);
			return View("Index", BuildViewModel());
		}

		private ViewModels.Admin.RoleIndex BuildViewModel()
		{
			ViewModels.Admin.RoleIndex viewModel = new();

			viewModel.Roles = this.RoleManager.List(this.Context.Site);
			
			return viewModel;
		}

		private ViewModels.Admin.RoleEditor BuildViewModel(Role role)
		{
			ViewModels.Admin.RoleEditor viewModel = new();

			viewModel.Role = role;					
			viewModel.RoleGroups = this.RoleGroupManager.List(this.Context.Site);							

			return viewModel;
		}
	}
}
