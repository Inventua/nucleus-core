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
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Extensions;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
	public class RolesController : Controller
	{
		private ILogger<RolesController> Logger { get; }
		private IRoleManager RoleManager { get; }
		private IUserManager UserManager { get; }
		private IRoleGroupManager RoleGroupManager { get; }
		private Context Context { get; }

		public RolesController(Context context, ILogger<RolesController> logger, IRoleManager roleManager, IUserManager userManager, IRoleGroupManager roleGroupManager)
		{
			this.Context = context;
			this.Logger = logger;
			this.RoleManager = roleManager;
			this.RoleGroupManager = roleGroupManager;
			this.UserManager = userManager;
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

			// Read role type from the database.  We don't want to remove existing type flags, but we also don't want to
			// add it as a <hidden> in the UI, because the other values are sensitive.
			if (viewModel.Role.Id != Guid.Empty)
			{
				viewModel.Role.Type = (await this.RoleManager.Get(viewModel.Role.Id))?.Type ?? Role.RoleType.Normal;
			}

			if (viewModel.IsAutoRole)
			{
				if (viewModel.Role.Type.HasFlag(Role.RoleType.Restricted))
				{
					// restricted roles can't have users added or removed
					return BadRequest("Role types restricted and auto cannot be combined.");
				}
				else
				{
					// add auto flag
					viewModel.Role.Type = viewModel.Role.Type | Role.RoleType.AutoAssign;
				}
			}
			else
			{
				// remove auto flag
				viewModel.Role.Type = viewModel.Role.Type &~ Role.RoleType.AutoAssign;
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

		[HttpPost]		
		public async Task<ActionResult> DeleteUserRole(ViewModels.Admin.RoleEditor viewModel, Guid userId)
		{
			User user = await this.UserManager.Get(userId);

			await this.UserManager.RemoveRole(user, viewModel.Role.Id);
			await this.UserManager.Save(this.Context.Site, user);	

			return await ListUsersInRole(viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> ListUsersInRole(ViewModels.Admin.RoleEditor viewModel)
		{
			viewModel.Site = this.Context.Site;

			if (!viewModel.Role.Type.HasFlag(Role.RoleType.Restricted))
			{
				// read users
				viewModel.Users = await this.UserManager.ListUsersInRole(viewModel.Role, viewModel.Users);
			}

			return View("_UsersList", viewModel);
		}

		/// <summary>
		/// Export all roles to excel.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Export()
		{
			IEnumerable<Role> roles = await this.RoleManager.List(this.Context.Site);

			var exporter = new Nucleus.Extensions.ExcelWriter<Role>();

			exporter.AddColumn(role => role.Id);
			exporter.AddColumn(role => role.Name);
			exporter.AddColumn(role => role.Description);

			// exporter.AddColumn("TEST", "TEST", ClosedXML.Excel.XLDataType.DateTime, () => DateTime.Now);
			exporter.AddColumn("RoleGroup", "Role Group", ClosedXML.Excel.XLDataType.Text, role => role.RoleGroup == null ? "" : role.RoleGroup.Name);

			exporter.AddColumn(role => role.Type);
			exporter.AddColumn(role => role.DateAdded);
			exporter.AddColumn(role => role.DateChanged);

			exporter.Worksheet.Name = "Roles";
			exporter.Export(roles);

			return File(exporter.GetOutputStream(), ExcelWriter.MIMETYPE_EXCEL, $"Roles Export {DateTime.Now}.xlsx");
		}

		/// <summary>
		/// Export all roles to excel.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> ExportRoleUsers(Guid id)
		{
			Role role = await this.RoleManager.Get(id);
			IList<User> users = await this.UserManager.ListUsersInRole(role);

			var exporter = new Nucleus.Extensions.ExcelWriter<User>
			(
				ExcelWriter.Modes.IncludeSpecifiedPropertiesOnly
			);

			exporter.AddColumn(user => user.Id);
			exporter.AddColumn(user => user.UserName);
			exporter.AddColumn(user => user.Approved);
			exporter.AddColumn(user => user.Verified);
			exporter.AddColumn(user => user.Roles);
			
			foreach (UserProfileProperty profileProperty in this.Context.Site.UserProfileProperties)
			{
				exporter.AddColumn(profileProperty.Name, profileProperty.Name, ClosedXML.Excel.XLDataType.Text,
					user => user.Profile
						.Where(profile => profile.UserProfileProperty.Id == profileProperty.Id)
						.Select(profileValue => profileValue.Value)
						.FirstOrDefault());
			}

			exporter.AddColumn(user => user.DateAdded);
			exporter.AddColumn(user => user.DateChanged);

			exporter.Worksheet.Name = "Users";
			exporter.Export(users);

			return File(exporter.GetOutputStream(), ExcelWriter.MIMETYPE_EXCEL, $"Role {role.Name} Users Export {DateTime.Now}.xlsx");
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

			viewModel.Site = this.Context.Site;

			viewModel.Role = role;
			viewModel.IsAutoRole = role.Type.HasFlag(Role.RoleType.AutoAssign);

			viewModel.RoleGroups = await this.RoleGroupManager.List(this.Context.Site);

			if(!viewModel.Role.Type.HasFlag(Role.RoleType.Restricted))
			{
				// read users
				viewModel.Users = await this.UserManager.ListUsersInRole(viewModel.Role, viewModel.Users);
			}

			return viewModel;
		}
	}
}
