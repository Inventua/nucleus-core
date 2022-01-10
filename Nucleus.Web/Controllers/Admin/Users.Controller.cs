using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
	public class UsersController : Controller
	{
		private ILogger<UsersController> Logger { get; }
		private IUserManager UserManager { get; }
		private IRoleManager RoleManager { get; }
		private ClaimTypeOptions ClaimTypeOptions { get; }

		private Context Context { get; }

		public UsersController(Context context, ILogger<UsersController> logger, IUserManager userManager, IRoleManager roleManager, IOptions< ClaimTypeOptions> claimTypeOptions)
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
		public async Task<ActionResult> Index()
		{
			return View("Index", await BuildViewModel());
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public async Task<ActionResult> Search(ViewModels.Admin.UserIndex viewModel)
		{
			viewModel.SearchResults = await this.UserManager.Search(this.Context.Site, viewModel.SearchTerm, viewModel.SearchResults);
			
			return View("SearchResults", viewModel);
		}

		/// <summary>
		/// Display the user editor
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Editor(Guid id)
		{
			ViewModels.Admin.UserEditor viewModel;

			viewModel = await BuildViewModel(id == Guid.Empty ? await this.UserManager.CreateNew(this.Context.Site) : await this.UserManager.Get(this.Context.Site, id));

			return View("Editor", viewModel);
		}

		[HttpPost]
		public async Task<ActionResult> AddUser()
		{
			return View("Editor", await BuildViewModel(new User()));
		}

		[HttpPost]
		public async Task<ActionResult> AddRole(ViewModels.Admin.UserEditor viewModel)
		{
			await this.UserManager.AddRole(viewModel.User, viewModel.SelectedRoleId);
			//this.UserManager.SetupUserProfileProperties(viewModel.User);
			return View("Editor", await BuildViewModel(viewModel.User));
		}

		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Admin.UserEditor viewModel)
		{			
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			// only save a password for a new user (and if they entered one)
			if (viewModel.User.Id == Guid.Empty && !String.IsNullOrEmpty(viewModel.EnteredPassword))
			{
				Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = await this.UserManager.ValidatePasswordComplexity(nameof(viewModel.EnteredPassword), viewModel.EnteredPassword);
				if (!modelState.IsValid)
				{
					return BadRequest(modelState);
				}

				if (viewModel.User.Secrets == null)
				{
					viewModel.User.Secrets = new();
				}
				viewModel.User.Secrets.SetPassword(viewModel.EnteredPassword);
			}

			// If the user has no roles assigned (or they have all been deleted), model binding will set .Roles to null - which is interpreted
			// in the data provider as meaning "don't save role assignments".  We need to set roles to an empty list so that role assignments are
			// saved (that is, existing roles are removed).
			if (viewModel.User.Roles == null)
			{
				viewModel.User.Roles = new();
			}

			await this.UserManager.Save(this.Context.Site, viewModel.User);

			return View("Index", await BuildViewModel());
		}


		[HttpPost]
		public async Task<ActionResult> RemoveUserRole(ViewModels.Admin.UserEditor viewModel, Guid roleId)
		{
			await this.UserManager.RemoveRole(viewModel.User, roleId);
			this.ControllerContext.ModelState.RemovePrefix("User.Roles");
			return View("Editor", await BuildViewModel(viewModel.User));
		}

		[HttpPost]
		public async Task<ActionResult> DeleteUser(ViewModels.Admin.UserEditor viewModel)
		{
			await this.UserManager.Delete(viewModel.User);
			return View("Index", await BuildViewModel());
		}

		private async Task<ViewModels.Admin.UserIndex> BuildViewModel()
		{
			ViewModels.Admin.UserIndex viewModel = new();

			viewModel.Users = await this.UserManager.List(this.Context.Site);

			return viewModel;
		}

		private async Task<ViewModels.Admin.UserEditor> BuildViewModel(User user)
		{
			ViewModels.Admin.UserEditor viewModel = new();

			viewModel.User = user;
			viewModel.ClaimTypeOptions = this.ClaimTypeOptions;
			
			viewModel.IsCurrentUser = (user.Id == ControllerContext.HttpContext.User.GetUserId());
			if (viewModel.User != null)
			{
				foreach (Role role in await this.RoleManager.List(this.Context.Site))
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
