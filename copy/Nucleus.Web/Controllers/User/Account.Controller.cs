using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Nucleus.Core.DataProviders;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.Authentication;
using Nucleus.Core;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Core.Authorization;
using Nucleus.ViewFeatures;

namespace Nucleus.Web.Controllers
{
	[Area(AREA_NAME)]
	public class AccountController : Controller
	{
		public const string AREA_NAME = "User";

		private Context Context { get; }
		private ILogger<AccountController> Logger { get; }
		private UserManager UserManager { get; }
		private SessionManager SessionManager { get; }
		private PageManager PageManager { get; }

		private ClaimTypeOptions ClaimTypeOptions { get; }

		public AccountController(Context context, ILogger<AccountController> Logger, UserManager userManager, SessionManager sessionManager, PageManager pageManager, IOptions<ClaimTypeOptions> claimTypeOptions)
		{
			this.Context = context;
			this.Logger = Logger;
			this.UserManager = userManager;
			this.PageManager = pageManager;
			this.SessionManager = sessionManager;
			this.ClaimTypeOptions = claimTypeOptions.Value;
		}

		public ActionResult Index(string returnUrl)
		{
			return View("Login", new ViewModels.User.AccountPassword() { ReturnUrl = returnUrl });
		}

		public ActionResult Menu()
		{
			ViewModels.User.AccountMenu viewModel = new();
			SitePages sitePage = this.Context.Site.GetSitePages();

			PageRoute userProfilePageRoute = null;

			if (sitePage.UserProfilePageId.HasValue)
			{
				Page profilePage = this.PageManager.Get(sitePage.UserProfilePageId.Value);
				if (profilePage != null)
				{
					userProfilePageRoute = profilePage.DefaultPageRoute();
				}
			}

			if (userProfilePageRoute == null)
			{
				viewModel.UserProfileUrl = Url.AreaAction("EditAccountSettings", "Account", "User");
			}
			else
			{
				viewModel.UserProfileUrl= userProfilePageRoute.Path;
			}

			return View("AccountMenu", viewModel);
		}

		[HttpPost]
		async public Task<ActionResult> Login(ViewModels.User.AccountPassword viewModel)
		{
			User loginUser;
			List<Claim> claims = new();

			loginUser = this.UserManager.Get(this.Context.Site, viewModel.Username);

			if (loginUser == null)
			{
				// try system admin
				loginUser = this.UserManager.GetSystemAdministrator(viewModel.Username);
			}

			if (loginUser == null)
			{
				viewModel.Message = "Invalid username or password.";
			}
			else
			{
				if (!this.UserManager.VerifyPassword(loginUser, viewModel.Password))
				{
					viewModel.Message = "Invalid username or password.";
				}
				else
				{
					UserSession session = this.SessionManager.CreateNew(this.Context.Site, loginUser, viewModel.RememberMe, ControllerContext.HttpContext.Connection.RemoteIpAddress);
					await this.SessionManager.SignIn(session, HttpContext, viewModel.ReturnUrl);

					return Redirect(String.IsNullOrEmpty(viewModel.ReturnUrl) ? Url.GetAbsoluteUri("/").ToString() : Url.GetAbsoluteUri(viewModel.ReturnUrl).ToString());
				}
			}

			return View("Login", viewModel);
		}

		public async Task<ActionResult> Logout(string returnUrl)
		{
			await this.SessionManager.SignOut(HttpContext);
			//await HttpContext.SignOutAsync();

			return Redirect(String.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
		}

		[HttpGet]
		public ActionResult EditPassword(string returnUrl)
		{
			return View("ChangePassword", new ViewModels.User.AccountPassword() { ReturnUrl = returnUrl });
		}

		[HttpPost]
		public ActionResult ChangePassword(ViewModels.User.AccountPassword viewModel)
		{
			User loginUser;
			List<Claim> claims = new();

			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			if (viewModel.ConfirmPassword!= viewModel.NewPassword)
			{
				return BadRequest("Your new password and confirm password values do not match");
			}

			if (User.IsSystemAdministrator())
			{
				loginUser = this.UserManager.GetSystemAdministrator(ControllerContext.HttpContext.User.Identity.Name);
			}
			else
			{
				loginUser = this.UserManager.Get(this.Context.Site, ControllerContext.HttpContext.User.Identity.Name);
			}

			if (loginUser == null)
			{
				viewModel.Message = "Not logged in.";
			}
			else
			{
				if (String.IsNullOrEmpty(viewModel.Password) || !loginUser.Secrets.VerifyPassword(viewModel.Password))
				{
					viewModel.Message = "Invalid password.";
				}
				else
				{
					Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = this.UserManager.ValidatePasswordComplexity(viewModel.NewPassword);
					if (!modelState.IsValid)
					{
						viewModel.Message = modelState.ToErrorString();
					}
					// null password is a "last resort" check in case all password complexity rules have been removed
					else if (!String.IsNullOrEmpty(viewModel.NewPassword)) 
					{
						loginUser.Secrets.SetPassword(viewModel.NewPassword);
						this.UserManager.Save(this.Context.Site, loginUser);
						return Redirect(String.IsNullOrEmpty(viewModel.ReturnUrl) ? "/" : viewModel.ReturnUrl);
					}
				}
			}

			return View("ChangePassword", viewModel);
		}

		[HttpGet]
		public ActionResult EditAccountSettings(string returnUrl)
		{
			return View("AccountSettings", new ViewModels.User.AccountSettings() 
			{
				User = this.UserManager.Get(this.Context.Site, this.ControllerContext.HttpContext.User.GetUserId()), 
				ClaimTypeOptions = this.ClaimTypeOptions,
				ReturnUrl = returnUrl 
			});
		}

		[HttpPost]
		public ActionResult SaveAccountSettings(ViewModels.User.AccountSettings viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			this.UserManager.Save(this.Context.Site, viewModel.User);
			
			return Redirect(String.IsNullOrEmpty(viewModel.ReturnUrl) ? "/" : viewModel.ReturnUrl);
		}
	}
}
