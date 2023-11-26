using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Managers;
using Nucleus.ViewFeatures;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
				await Task.Delay(TimeSpan.FromSeconds(10));
				return Json(new { Title = "Login", Message = "Invalid user name or password." });
			}
			else
			{
				if (!await this.UserManager.VerifyPassword(loginUser, viewModel.Password))
				{
					await Task.Delay(TimeSpan.FromSeconds(10));
					return Json(new { Title = "Login", Message = "Invalid user name or password." });
				}
				else
				{
					if (!loginUser.Approved)
					{
						return Json(new { Title = "Login", Message = "Your account has not been approved." });
					}
					else if (!loginUser.Verified && !viewModel.ShowVerificationToken)
					{
						ModelState.Remove(nameof(ViewModels.User.AccountPassword.ShowVerificationToken));
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
								await Task.Delay(TimeSpan.FromSeconds(10));
								return Json(new { Title = "Login", Message = "Invalid verification token." });
							}
						}

						UserSession session = await this.SessionManager.CreateNew(this.Context.Site, loginUser, false, ControllerContext.HttpContext.Connection.RemoteIpAddress);

            if (!Url.IsLocalUrl(viewModel.ReturnUrl)) viewModel.ReturnUrl = "";
            await this.SessionManager.SignIn(session, HttpContext, viewModel.ReturnUrl);            
            string location = String.IsNullOrEmpty(viewModel.ReturnUrl) ? Url.Content("~/") : viewModel.ReturnUrl;

						return Redirect(location);						
					}
				}
			}
		}

		public async Task<ActionResult> Logout(string returnUrl)
		{
			await this.SessionManager.SignOut(HttpContext);

      if (!Url.IsLocalUrl(returnUrl)) returnUrl = "";
      return Redirect(Url.Content(String.IsNullOrEmpty(returnUrl) ? "~/" : returnUrl));
		}

		[HttpGet]
		public ActionResult EditPassword(string returnUrl)		
    {			
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
				return Json(new { Title = "Login", Message = "Your new password and confirm password values do not match" });				
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
				ModelState.AddModelError<ViewModels.User.AccountPassword>(model => model.Password, "Invalid password.");
        return BadRequest(ModelState);
      }
			else
			{
				if (String.IsNullOrEmpty(viewModel.Password) || !loginUser.Secrets.VerifyPassword(viewModel.Password))
				{
					ModelState.AddModelError<ViewModels.User.AccountPassword>(model => model.Password, "Invalid password.");
          return BadRequest(ModelState);
        }
				else
				{
					Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = await this.UserManager.ValidatePasswordComplexity(nameof(viewModel.NewPassword), viewModel.NewPassword);
          if (!modelState.IsValid)
          {
            return BadRequest(modelState);
          }
          else if (String.IsNullOrEmpty(viewModel.NewPassword))
          {
            // null password check is a "last resort" in case all password complexity rules have been removed from config
            ModelState.AddModelError<ViewModels.User.AccountPassword>(model => model.Password, "Invalid password.");
            return BadRequest(ModelState);
          }
          else 
          {
						await this.UserManager.SetPassword(loginUser, viewModel.NewPassword);

            if (!Url.IsLocalUrl(viewModel.ReturnUrl)) viewModel.ReturnUrl = "";
            return Redirect(Url.Content(String.IsNullOrEmpty(viewModel.ReturnUrl) ? "~/" : viewModel.ReturnUrl));
					}
				}
			}
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
      return Redirect(Url.Content(String.IsNullOrEmpty(viewModel.ReturnUrl) ? "~/" : viewModel.ReturnUrl));
		}
	}
}
