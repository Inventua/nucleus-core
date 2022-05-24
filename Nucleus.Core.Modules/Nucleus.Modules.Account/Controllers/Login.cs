using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Nucleus.ViewFeatures;
using Nucleus.Extensions;

namespace Nucleus.Modules.Account.Controllers
{
	[Extension("Account")]
	public class LoginController : Controller
	{
		private Context Context { get; }
		private ILogger<LoginController> Logger { get; }
		private IUserManager UserManager { get; }
		private ISessionManager SessionManager { get; }
		private IPageManager PageManager { get; }
		private IPageModuleManager PageModuleManager { get; }

		private ClaimTypeOptions ClaimTypeOptions { get; }

		internal class ModuleSettingsKeys
		{
			public const string AllowRememberMe = "login:allowrememberme";
			public const string AllowUsernameRecovery = "login:allowusernamerecovery";
			public const string AllowPasswordReset = "login:allowpasswordreset";			
		}

		public LoginController(Context context, ILogger<LoginController> Logger, IUserManager userManager, ISessionManager sessionManager, IPageManager pageManager, IPageModuleManager pageModuleManager, IOptions<ClaimTypeOptions> claimTypeOptions)
		{
			this.Context = context;
			this.Logger = Logger;
			this.UserManager = userManager;
			this.PageManager = pageManager;
			this.PageModuleManager = pageModuleManager;
			this.SessionManager = sessionManager;
			this.ClaimTypeOptions = claimTypeOptions.Value;
		}

		[HttpGet]
		public ActionResult Index(string returnUrl, string token, string reason)
		{
			ViewModels.Login viewModel = BuildViewModel(returnUrl);

			switch (reason)
			{
				case nameof(System.Net.HttpStatusCode.Forbidden):
					viewModel.Message = "Access was denied.  Your account is not authorized to use this system.";
					break;
									
				default:
					viewModel.Message = "";
					break;
			}
			

			if (!String.IsNullOrEmpty(token) && viewModel.AllowPasswordReset)
			{
				return View("ResetPassword", new ViewModels.ResetPassword { PasswordResetToken = token });
			}
			else
			{
				return View("Login", viewModel);
			}
		}

		[HttpGet]
		public ActionResult Edit(string returnUrl)
		{
			return View("Settings", BuildViewModel(""));
		}

		[HttpPost]
		public ActionResult SaveSettings(ViewModels.Login viewModel)
		{
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.AllowPasswordReset, viewModel.AllowPasswordReset);
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.AllowUsernameRecovery, viewModel.AllowUsernameRecovery);
			this.Context.Module.ModuleSettings.Set(ModuleSettingsKeys.AllowRememberMe, viewModel.AllowRememberMe);

			this.PageModuleManager.SaveSettings(this.Context.Module);

			return Ok();
		}

		[HttpPost]
		async public Task<ActionResult> Login(ViewModels.Login viewModel)
		{
			User loginUser;
			List<Claim> claims = new();

			loginUser = await this.UserManager.Get(this.Context.Site, viewModel.Username);

			if (loginUser == null)
			{
				// try system admin
				loginUser = await this.UserManager.GetSystemAdministrator(viewModel.Username);
			}

			if (loginUser == null)
			{
				return Json(new { Title = "Login", Message = "Invalid username or password." });
			}
			else
			{
				if (!await this.UserManager.VerifyPassword(loginUser, viewModel.Password))
				{
					return Json(new { Title = "Login", Message = "Invalid username or password." });
				}
				else
				{
					if (!loginUser.Approved)
					{
						return Json(new { Title = "Login", Message = "Your account has not been approved." });
					}
					else if (!loginUser.Verified && !viewModel.ShowVerificationToken)
					{
						ModelState.Remove(nameof(ViewModels.Login.ShowVerificationToken));
						viewModel.ShowVerificationToken = true;
						return View(viewModel);						
					}
					else
					{
						if (viewModel.ShowVerificationToken && viewModel.VerificationToken != null)
						{
							if (viewModel.VerificationToken == loginUser.Secrets.VerificationToken)
							{
								if (loginUser.Secrets.VerificationTokenExpiryDate < DateTime.UtcNow)
								{
									return Json(new { Title = "Login", Message = "Your verification token has expired. Contact the system administrator for help." });
								}
								else
								{
									// mark user as verified
									loginUser.Verified = true;
									await this.UserManager.Save(this.Context.Site, loginUser);
								}
							}
							else
							{
								return Json(new { Title = "Login", Message = "Invalid verification token." });
							}
						}
						
						UserSession session = await this.SessionManager.CreateNew(this.Context.Site, loginUser, viewModel.AllowRememberMe && viewModel.RememberMe, ControllerContext.HttpContext.Connection.RemoteIpAddress);
						await this.SessionManager.SignIn(session, HttpContext, viewModel.ReturnUrl);

						string location = String.IsNullOrEmpty(viewModel.ReturnUrl) ? Url.Content("~/").ToString() :viewModel.ReturnUrl;
						ControllerContext.HttpContext.Response.Headers.Add("X-Location", Url.Content(location));
						return StatusCode((int)System.Net.HttpStatusCode.Found);
					}
				}
			}
		}

		public async Task<ActionResult> Logout(string returnUrl)
		{
			await this.SessionManager.SignOut(HttpContext);
			
			return Redirect(String.IsNullOrEmpty(returnUrl) ? "~/" : returnUrl);
		}

		private ViewModels.Login BuildViewModel(string returnUrl)
		{
			return new ViewModels.Login()
			{
				AllowPasswordReset = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.AllowPasswordReset, true),
				AllowUsernameRecovery = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.AllowUsernameRecovery, true),
				AllowRememberMe = this.Context.Module.ModuleSettings.Get(ModuleSettingsKeys.AllowRememberMe, true),
				ReturnUrl=returnUrl
			};
		}

	}
}
