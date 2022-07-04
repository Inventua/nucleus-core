using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY)]
	public class SystemAdministratorsController : Controller
	{
		private ILogger<UsersController> Logger { get; }
		private IUserManager UserManager { get; }
		private IRoleManager RoleManager { get; }
		private ClaimTypeOptions ClaimTypeOptions { get; }

		private Context Context { get; }

		public SystemAdministratorsController(Context context, ILogger<UsersController> logger, IUserManager userManager, IOptions<ClaimTypeOptions> claimTypeOptions)
		{
			this.Context = context;
			this.Logger = logger;
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

			return View("Editor", await BuildViewModel(newUser));
		}

	
		[HttpPost]
		public async Task<ActionResult> Save(ViewModels.Admin.UserEditor viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			// only save a password for a new user (and if they entered one)
			if (viewModel.User.Id == Guid.Empty && String.IsNullOrEmpty(viewModel.EnteredPassword))
			{
				return BadRequest("System Administrator users must have a password.");
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

			// only save a password for a new user
			if (viewModel.User.Id == Guid.Empty)
			{
				if (viewModel.User.Secrets == null)
				{
					viewModel.User.Secrets = new();
				}
				viewModel.User.Secrets.SetPassword(viewModel.EnteredPassword);
			}
					
			await this.UserManager.SaveSystemAdministrator(viewModel.User);

			return View("Index", await BuildViewModel());
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
			viewModel.Users = await this.UserManager.ListSystemAdministrators(viewModel.Users);

			return viewModel;
		}

		private Task<ViewModels.Admin.UserEditor> BuildViewModel(User user)
		{
			ViewModels.Admin.UserEditor viewModel = new();

			viewModel.User = user;
			viewModel.ClaimTypeOptions = this.ClaimTypeOptions;

			viewModel.IsCurrentUser = (user.Id == ControllerContext.HttpContext.User.GetUserId());
			
			return Task.FromResult(viewModel);
		}
	}
}
