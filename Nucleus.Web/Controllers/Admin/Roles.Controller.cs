using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
	public class RolesController : Controller
	{
		private ILogger<RolesController> Logger { get; }
		private IRoleManager RoleManager { get; }
		private IRoleGroupManager RoleGroupManager { get; }
		private Context Context { get; }

		public RolesController(Context context, ILogger<RolesController> logger, IRoleManager roleManager, IRoleGroupManager roleGroupManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.RoleManager = roleManager;
			this.RoleGroupManager = roleGroupManager;
		}

		/// <summary>
		/// Display the roles list
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Index()
		{
			return View("Index", await BuildViewModel ());
		}

		/// <summary>
		/// Display the roles list
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public async Task<ActionResult> List(ViewModels.Admin.RoleIndex viewModel)
		{
			return View("_RolesList", await BuildViewModel(viewModel));
		}

		/// <summary>
		/// Display the role editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Editor(Guid id)
		{
			ViewModels.Admin.RoleEditor viewModel;

			viewModel = await BuildViewModel(id == Guid.Empty ? await RoleManager.CreateNew() : await RoleManager.Get(id));
			
			return View("Editor", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> AddRole()
		{
			return View("Editor", await BuildViewModel(new Role()));
		}

		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Admin.RoleEditor viewModel)
		{
			if (viewModel.Role.RoleGroup.Id == Guid.Empty)
			{
				viewModel.Role.RoleGroup = null;
				ControllerContext.ModelState.Remove("Role.RoleGroup.Id");
			}
			ControllerContext.ModelState.Remove("Role.RoleGroup.Name");

			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			await this.RoleManager.Save(this.Context.Site, viewModel.Role);
			
			return View("Index", await BuildViewModel());
		}

		[HttpPost]
		public async Task<ActionResult> DeleteRole(ViewModels.Admin.RoleEditor viewModel)
		{
			if (viewModel.Role.RoleGroup.Id == Guid.Empty)
			{
				viewModel.Role.RoleGroup = null;				
			}
			await this.RoleManager.Delete(viewModel.Role);
			return View("Index", await BuildViewModel());
		}

		private async Task<ViewModels.Admin.RoleIndex> BuildViewModel()
		{
			return await BuildViewModel(new ViewModels.Admin.RoleIndex());
		}

		private async Task<ViewModels.Admin.RoleIndex> BuildViewModel(ViewModels.Admin.RoleIndex viewModel)
		{
			viewModel.Roles = await this.RoleManager.List(this.Context.Site, viewModel.Roles);

			return viewModel;
		}

		private async Task<ViewModels.Admin.RoleEditor> BuildViewModel(Role role)
		{
			ViewModels.Admin.RoleEditor viewModel = new();

			viewModel.Role = role;					
			viewModel.RoleGroups = await this.RoleGroupManager.List(this.Context.Site);							

			return viewModel;
		}
	}
}
