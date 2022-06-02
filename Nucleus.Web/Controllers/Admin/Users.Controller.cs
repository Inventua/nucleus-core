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
		/// Display the user list
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult> Index()
		{
			return View("Index", await BuildViewModel());
		}

		/// <summary>
		/// Display the user list
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public async Task<ActionResult> List(ViewModels.Admin.UserIndex viewModel)
		{
			return View("_UserList", await BuildViewModel(viewModel));
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

		[HttpGet]
		public async Task<ActionResult> AddUser()
		{
			User newUser = await this.UserManager.CreateNew(this.Context.Site);
			newUser.Verified = true;
			newUser.Approved = true;

			return View("Editor", await BuildViewModel( newUser ));
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
			if (viewModel.User.Id == Guid.Empty)
			{
				if (viewModel.User.Secrets == null)
				{
					viewModel.User.Secrets = new();
				}

				if (!String.IsNullOrEmpty(viewModel.EnteredPassword))
        {
					Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = await this.UserManager.ValidatePasswordComplexity(nameof(viewModel.EnteredPassword), viewModel.EnteredPassword);
					if (!modelState.IsValid)
					{
						return BadRequest(modelState);
					}
								
					viewModel.User.Secrets.SetPassword(viewModel.EnteredPassword);
				}
        else
        {
					return BadRequest("Please enter a password.");
        }
			}
			else
      {
				// We must NULL viewModel.User.Secrets for existing users because model binding always creates a new (empty) .Secrets object, which would cause
				// the data provider to overwrite an existing user's secrets record with blanks.  Setting to null prevents the data provider 
				// from saving secrets.
				viewModel.User.Secrets = null;
			}

			// If the user has no roles assigned (or they have all been deleted), model binding will set .Roles to null - which is interpreted
			// in the data provider as meaning "don't save role assignments".  We need to set roles to an empty list so that role assignments are
			// saved (that is, existing roles are removed).
			if (viewModel.User.Roles == null)
			{
				viewModel.User.Roles = new();
			}

			// If the user being edited is the currently logged in user, the approved and verified fields won't be shown, so we need
			// to get them  from the existing record
			if (viewModel.User.Id == ControllerContext.HttpContext.User.GetUserId())
			{
				User existing = await this.UserManager.Get(this.Context.Site, viewModel.User.Id);
				if (existing != null)
				{
					viewModel.User.Verified = existing.Verified;
					viewModel.User.Approved = existing.Approved;
				}
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
			return await BuildViewModel(new ViewModels.Admin.UserIndex());
		}

		private async Task<ViewModels.Admin.UserIndex> BuildViewModel(ViewModels.Admin.UserIndex viewModel)
		{
			viewModel.Users = await this.UserManager.List(this.Context.Site, viewModel.Users);
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
					if (!((role.Type & Role.RoleType.Restricted) == Role.RoleType.Restricted) && !viewModel.User.Roles?.Contains(role) == true)
					{
						viewModel.AvailableRoles.Add(role);
					}
				}
			}

			return viewModel;
		}
	}
}
