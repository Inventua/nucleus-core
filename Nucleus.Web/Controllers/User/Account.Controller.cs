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
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Managers;
using Nucleus.ViewFeatures;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions;

namespace Nucleus.Web.Controllers
{
	[Area(AREA_NAME)]
	public class AccountController : Controller
	{
		public const string AREA_NAME = "User";

		private Context Context { get; }
		private ILogger<AccountController> Logger { get; }
		private IUserManager UserManager { get; }
		private ISessionManager SessionManager { get; }
		private IPageManager PageManager { get; }

		private ClaimTypeOptions ClaimTypeOptions { get; }

		public AccountController(Context context, ILogger<AccountController> Logger, IUserManager userManager, ISessionManager sessionManager, IPageManager pageManager, IOptions<ClaimTypeOptions> claimTypeOptions)
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

		public async Task<ActionResult> Menu()
		{
			ViewModels.User.AccountMenu viewModel = new();
			SitePages sitePage = this.Context.Site.GetSitePages();

			PageRoute userProfilePageRoute = null;

			if (sitePage.UserProfilePageId.HasValue)
			{
				Page profilePage = await this.PageManager.Get(sitePage.UserProfilePageId.Value);
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
		public async Task<ActionResult> Login(ViewModels.User.AccountPassword viewModel)
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
				viewModel.Message = "Invalid username or password.";
			}
			else
			{
				if (!await this.UserManager.VerifyPassword(loginUser, viewModel.Password))
				{
					viewModel.Message = "Invalid username or password.";
				}
				else
				{
					UserSession session = await this.SessionManager.CreateNew(this.Context.Site, loginUser, viewModel.RememberMe, ControllerContext.HttpContext.Connection.RemoteIpAddress);
					await this.SessionManager.SignIn(session, HttpContext, viewModel.ReturnUrl);

					return Redirect(String.IsNullOrEmpty(viewModel.ReturnUrl) ? Url.GetAbsoluteUri("/").ToString() : Url.GetAbsoluteUri(viewModel.ReturnUrl).ToString());
				}
			}

			return View("Login", viewModel);
		}

		public async Task<ActionResult> Logout(string returnUrl)
		{
			await this.SessionManager.SignOut(HttpContext);
			
			return Redirect(String.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
		}

		[HttpGet]
		public ActionResult EditPassword(string returnUrl)		{
			
			return View("ChangePassword", new ViewModels.User.AccountPassword() { ReturnUrl = returnUrl });
		}

		[HttpPost]
		public async Task<ActionResult> ChangePassword(ViewModels.User.AccountPassword viewModel)
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
				loginUser = await this.UserManager.GetSystemAdministrator(ControllerContext.HttpContext.User.Identity.Name);
			}
			else
			{
				loginUser = await this.UserManager.Get(this.Context.Site, ControllerContext.HttpContext.User.Identity.Name);
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
					Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = await this.UserManager.ValidatePasswordComplexity(nameof(viewModel.NewPassword), viewModel.NewPassword);
					if (!modelState.IsValid)
					{
						viewModel.Message = modelState.ToErrorString();
					}
					// null password is a "last resort" check in case all password complexity rules have been removed
					else if (!String.IsNullOrEmpty(viewModel.NewPassword)) 
					{
						loginUser.Secrets.SetPassword(viewModel.NewPassword);
						await this.UserManager.SaveSecrets(loginUser);
						return Redirect(String.IsNullOrEmpty(viewModel.ReturnUrl) ? "/" : viewModel.ReturnUrl);
					}
				}
			}

			return View("ChangePassword", viewModel);
		}

		[HttpGet]
		public async Task<ActionResult> EditAccountSettings(string returnUrl)
		{
			return View("AccountSettings", new ViewModels.User.AccountSettings() 
			{
				User = await this.UserManager.Get(this.Context.Site, this.ControllerContext.HttpContext.User.GetUserId()), 
				ClaimTypeOptions = this.ClaimTypeOptions,
				ReturnUrl = returnUrl
			});
		}

		[HttpPost]
		public async Task<ActionResult> SaveAccountSettings(ViewModels.User.AccountSettings viewModel)
		{
			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			await this.UserManager.Save(this.Context.Site, viewModel.User);
			
			return Redirect(String.IsNullOrEmpty(viewModel.ReturnUrl) ? "/" : viewModel.ReturnUrl);
		}
	}
}
