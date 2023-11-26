using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Modules.Account.Controllers
{
	[Extension("Account")]
	public class UserProfileController : Controller
	{
		private Context Context { get; }
		private ILogger<LoginController> Logger { get; }
		private IUserManager UserManager { get; }
		private IPageManager PageManager { get; }
		private ClaimTypeOptions ClaimTypeOptions { get; }

		public UserProfileController(Context context, ILogger<LoginController> Logger, IUserManager userManager, IPageManager pageManager, IOptions<ClaimTypeOptions> claimTypeOptions)
		{
			this.Context = context;
			this.Logger = Logger;
			this.UserManager = userManager;
			this.PageManager = pageManager;
			this.ClaimTypeOptions = claimTypeOptions.Value;
		}

		[HttpGet]
		public async Task<ActionResult> Index(string returnUrl)
		{
      return View("UserProfile", new ViewModels.UserProfile()
			{
				User = await this.UserManager.Get(this.Context.Site, this.ControllerContext.HttpContext.User.GetUserId()),
				ClaimTypeOptions = this.ClaimTypeOptions,
				ReturnUrl = returnUrl
			});
		}

		[HttpGet]
		public ActionResult Edit()
		{
			return View("UserProfileSettings", new ViewModels.UserProfile());
		}

		[HttpPost]
		public async Task<ActionResult> SaveAccountSettings(ViewModels.UserProfile viewModel)
		{
			this.ControllerContext.ModelState.Remove("User.UserName");

			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			if (viewModel.User.Id != ControllerContext.HttpContext.User.GetUserId())
			{
				return BadRequest();
			}

			User existing = await this.UserManager.Get(this.Context.Site, HttpContext.User.GetUserId());
			existing.Profile = viewModel.User.Profile;

			if (existing.IsSystemAdministrator)
			{
				await this.UserManager.SaveSystemAdministrator(existing);
			}
			else
			{
				await this.UserManager.Save(this.Context.Site, existing); 
			}


      if (!Url.IsLocalUrl(viewModel.ReturnUrl)) viewModel.ReturnUrl = "";
      string location = String.IsNullOrEmpty(viewModel.ReturnUrl) ? Url.Content("~/") : viewModel.ReturnUrl;

			ControllerContext.HttpContext.Response.Headers.Add("X-Location", location);
			return StatusCode((int)System.Net.HttpStatusCode.Found);
		}
	}
}
