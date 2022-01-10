using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Core;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Core.Authorization;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Core.Authorization.SiteAdminAuthorizationHandler.SITE_ADMIN_POLICY)]
	public class UsersController : Controller
	{
		private ILogger<UsersController> Logger { get; }
		private UserManager UserManager { get; }
		private RoleManager RoleManager { get; }
		private ClaimTypeOptions ClaimTypeOptions { get; }

		private Context Context { get; }

		public UsersController(Context context, ILogger<UsersController> logger, UserManager userManager, RoleManager roleManager, IOptions< ClaimTypeOptions> claimTypeOptions)
		{
			this.Context = context;
			this.Logger = logger;
			this.RoleManager = roleManager;
			this.UserManager = userManager;
			this.ClaimTypeOptions = claimTypeOptions.Value;
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
		/// 
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Search(ViewModels.Admin.UserIndex viewModel)
		{
			viewModel.Users = this.UserManager.Search(this.Context.Site, viewModel.SearchTerm);
			
			return View("SearchResults", viewModel);
		}

		/// <summary>
		/// Display the user editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public ActionResult Editor(Guid id)
		{
			ViewModels.Admin.UserEditor viewModel;

			viewModel = BuildViewModel(id == Guid.Empty ? this.UserManager.CreateNew(this.Context.Site) : this.UserManager.Get(this.Context.Site, id));

			return View("Editor", viewModel);
		}

		[HttpPost]
		public ActionResult AddUser()
		{
			return View("Editor", BuildViewModel(new User()));
		}

		[HttpPost]
		public ActionResult AddRole(ViewModels.Admin.UserEditor viewModel)
		{
			this.UserManager.AddRole(viewModel.User, viewModel.SelectedRoleId);
			//this.UserManager.SetupUserProfileProperties(viewModel.User);
			return View("Editor", BuildViewModel(viewModel.User));
		}

		[HttpPost]
		public ActionResult Save(ViewModels.Admin.UserEditor viewModel)
		{
			// Model binding to DropdownLists (using DropDownListFor) doesn't populate the role name property, which
			// makes the model invalid. TODO: Investigate custom model binding to solve this.
			//if (viewModel.User.Roles != null)
			//{
			//	foreach (Role role in viewModel.User.Roles)
			//	{
			//		role.Name = this.RoleManager.Get(role.Id)?.Name;
			//	}
			//}

			//ControllerContext.ModelState.Clear();
			//TryValidateModel(viewModel);
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			// only save a password for a new user (and if they entered one)
			if (viewModel.User.Id == Guid.Empty && !String.IsNullOrEmpty(viewModel.EnteredPassword))
			{
				if (viewModel.User.Secrets == null)
				{
					viewModel.User.Secrets = new();
				}
				viewModel.User.Secrets.SetPassword(viewModel.EnteredPassword);
			}

			this.UserManager.Save(this.Context.Site, viewModel.User);

			return View("Index", BuildViewModel());
		}


		[HttpPost]
		public ActionResult RemoveUserRole(ViewModels.Admin.UserEditor viewModel, Guid roleId)
		{
			this.UserManager.RemoveRole(viewModel.User, roleId);

			return View("Editor", BuildViewModel(viewModel.User));
		}

		[HttpPost]
		public ActionResult DeleteUser(ViewModels.Admin.UserEditor viewModel)
		{
			this.UserManager.Delete(viewModel.User);
			return View("Index", BuildViewModel());
		}

		private ViewModels.Admin.UserIndex BuildViewModel()
		{
			ViewModels.Admin.UserIndex viewModel = new ViewModels.Admin.UserIndex();

			viewModel.Users = this.UserManager.List(this.Context.Site);

			return viewModel;
		}

		private ViewModels.Admin.UserEditor BuildViewModel(User user)
		{
			ViewModels.Admin.UserEditor viewModel = new ViewModels.Admin.UserEditor();

			viewModel.User = user;
			viewModel.ClaimTypeOptions = this.ClaimTypeOptions;
			
			viewModel.IsCurrentUser = (user.Id == ControllerContext.HttpContext.User.GetUserId());
			if (viewModel.User != null)
			{
				foreach (Role role in this.RoleManager.List(this.Context.Site))
				{
					if (!((role.Type & Role.RoleType.Restricted) == Role.RoleType.Restricted) && !viewModel.User?.Roles.Contains(role) == true)
					{
						viewModel.AvailableRoles.Add(role);
					}
				}
			}

			return viewModel;
		}
	}
}
