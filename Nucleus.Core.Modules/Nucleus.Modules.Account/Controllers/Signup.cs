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
using Nucleus.ViewFeatures;

namespace Nucleus.Modules.Account.Controllers
{
	[Extension("Account")]
	public class SignupController : Controller
	{
		private Context Context { get; }
		private ILogger<LoginController> Logger { get; }
		private IUserManager UserManager { get; }
		private IPageManager PageManager { get; }

		private ClaimTypeOptions ClaimTypeOptions { get; }
		private ISessionManager SessionManager { get; }

		public SignupController(Context context, ILogger<LoginController> Logger, IUserManager userManager, IPageManager pageManager, ISessionManager sessionManager, IOptions<ClaimTypeOptions> claimTypeOptions)
		{
			this.Context = context;
			this.Logger = Logger;
			this.UserManager = userManager;
			this.PageManager = pageManager;
			this.SessionManager = sessionManager;
			this.ClaimTypeOptions = claimTypeOptions.Value;
		}

		[HttpGet]
		public async Task<ActionResult> Index(string returnUrl)
		{
			if (this.Context.Site.UserRegistrationOptions.HasFlag(Site.SiteUserRegistrationOptions.SignupAllowed))
			{
				return View("Signup", await BuildViewModel(returnUrl));
			}
			else
			{
				return Forbid();
			}
		}

		[HttpGet]
		public ActionResult Edit(string returnUrl)
		{
			return View("SignupSettings", new ViewModels.Signup());
		}

		[HttpPost]
		public async Task<ActionResult> Signup(ViewModels.Signup viewModel)
		{
			if (!this.ModelState.IsValid)
			{
				return BadRequest(this.ModelState);
			}

			if (viewModel.ConfirmPassword != viewModel.NewPassword)
			{
				return BadRequest("Your new password and confirm password values do not match");
			}

			Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = await this.UserManager.ValidatePasswordComplexity(nameof(viewModel.NewPassword), viewModel.NewPassword);
			if (!modelState.IsValid)
			{
				return BadRequest(modelState);
			}

			try
			{
				User newUser = await this.UserManager.CreateNew(this.Context.Site);
				this.UserManager.SetNewUserFlags(this.Context.Site, newUser);

				newUser.UserName = viewModel.User.UserName;
				newUser.Profile = viewModel.User.Profile;
				newUser.Secrets = new();
				newUser.Secrets.SetPassword(viewModel.NewPassword);

				if (!newUser.Verified)
				{
					await this.UserManager.SetVerificationToken(newUser);
				}

				await this.UserManager.Save(this.Context.Site, newUser);

				if (newUser.Approved && newUser.Verified)
				{
					UserSession session = await this.SessionManager.CreateNew(this.Context.Site, newUser, false, ControllerContext.HttpContext.Connection.RemoteIpAddress);
					await this.SessionManager.SignIn(session, HttpContext, viewModel.ReturnUrl);
				}

				string location = String.IsNullOrEmpty(viewModel.ReturnUrl) ? Url.Content("~/") : viewModel.ReturnUrl;
				ControllerContext.HttpContext.Response.Headers.Add("X-Location", location);
				return StatusCode((int)System.Net.HttpStatusCode.Found);

				//return Json(new { Title = "Sign Up", Message = "Your new account has been saved." });
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains("unique constraint failed", StringComparison.OrdinalIgnoreCase) || ex.InnerException?.Message.Contains("unique constraint failed", StringComparison.OrdinalIgnoreCase) == true)
				{
					return BadRequest("User name is already in use.");
				}
				else
				{
					throw;
				}
			}


		}

		private async Task<ViewModels.Signup> BuildViewModel(string returnUrl)
		{
			return new ViewModels.Signup()
			{
				ShowForm = true,
				ReturnUrl = returnUrl,
				User = await this.UserManager.CreateNew(this.Context.Site),
				ClaimTypeOptions = this.ClaimTypeOptions
			};
		}

	}
}
