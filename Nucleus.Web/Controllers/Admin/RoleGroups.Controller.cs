using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Extensions;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
	public class RoleGroupsController : Controller
	{
		private ILogger<RoleGroupsController> Logger { get; }
		private IRoleGroupManager RoleGroupManager { get; }
		private Context Context { get; }

		public RoleGroupsController(Context context, ILogger<RoleGroupsController> logger, IRoleGroupManager roleGroupManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.RoleGroupManager = roleGroupManager;
		}

		/// <summary>
		/// Display the role groups list
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Index()
		{
			return View("Index", await BuildViewModel());
		}

		/// <summary>
		/// Display the role groups list
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public async Task<ActionResult> List(ViewModels.Admin.RoleGroupIndex viewModel)
		{
			return View("_RoleGroupsList", await BuildViewModel(viewModel));
		}

		/// <summary>
		/// Display the role groups editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Editor(Guid id)
		{
			ViewModels.Admin.RoleGroupEditor viewModel;

			viewModel = await BuildViewModel(id == Guid.Empty ? await this.RoleGroupManager.CreateNew() : await this.RoleGroupManager.Get(id));
			
			return View("Editor", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> AddRoleGroup()
		{
			return View("Editor", await BuildViewModel(new RoleGroup()));
		}


		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Admin.RoleGroupEditor viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			await this.RoleGroupManager.Save(this.Context.Site, viewModel.RoleGroup);		

			return View("Index", await BuildViewModel());
		}

		[HttpPost]
		public async Task<ActionResult> DeleteRoleGroup(ViewModels.Admin.RoleGroupEditor viewModel)
		{
			await this.RoleGroupManager.Delete(viewModel.RoleGroup);
			return View("Index", await BuildViewModel());
		}

		/// <summary>
		/// Export all role groups to excel.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Export()
		{
			IEnumerable<RoleGroup> roleGroups = await this.RoleGroupManager.List(this.Context.Site);

			var exporter = new Nucleus.Extensions.ExcelWriter<RoleGroup>
			( 
				Extensions.ExcelWriter<RoleGroup>.Modes.AutoDetect,
				nameof(RoleGroup.AddedBy), 
				nameof(RoleGroup.ChangedBy)
			);
			exporter.Worksheet.Name = "Role Groups";
			exporter.Export(roleGroups);

			return File(exporter.GetOutputStream(), ExcelWriter.MIMETYPE_EXCEL, $"Role Groups Export {DateTime.Now}.xlsx");
		}

		private async Task<ViewModels.Admin.RoleGroupIndex> BuildViewModel()
		{
			return await BuildViewModel(new ViewModels.Admin.RoleGroupIndex());
		}

		private async Task<ViewModels.Admin.RoleGroupIndex> BuildViewModel(ViewModels.Admin.RoleGroupIndex viewModel)
		{
			viewModel.RoleGroups = await this.RoleGroupManager.List(this.Context.Site, viewModel.RoleGroups);

			return viewModel;
		}

		private Task<ViewModels.Admin.RoleGroupEditor> BuildViewModel(RoleGroup roleGroup)
		{
			ViewModels.Admin.RoleGroupEditor viewModel = new();

			viewModel.RoleGroup = roleGroup;
			
			return Task.FromResult(viewModel);
		}
	}
}
