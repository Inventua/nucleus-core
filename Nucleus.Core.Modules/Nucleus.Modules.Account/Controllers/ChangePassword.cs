﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Mail;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Nucleus.Extensions.Authorization;
using Nucleus.ViewFeatures;
using Nucleus.Abstractions.Models.Mail;
using Microsoft.AspNetCore.Routing;
using Nucleus.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nucleus.Modules.Account.Controllers
{
	[Extension("Account")]
	public class ChangePasswordController : Controller
	{
		private Context Context { get; }
		private ILogger<LoginController> Logger { get; }
		private IUserManager UserManager { get; }
		private IPageManager PageManager { get; }
		private IMailTemplateManager MailTemplateManager { get; }
		private IMailClientFactory MailClientFactory { get; }
		private LinkGenerator LinkGenerator { get; }

		public ChangePasswordController(Context context, ILogger<LoginController> Logger, LinkGenerator linkGenerator, IUserManager userManager, IPageManager pageManager, IMailTemplateManager mailTemplateManager, IMailClientFactory mailClientFactory)
		{
			this.Context = context;
			this.Logger = Logger;
			this.UserManager = userManager;
			this.PageManager = pageManager;
			this.MailTemplateManager = mailTemplateManager;
			this.MailClientFactory = mailClientFactory;
			this.LinkGenerator = linkGenerator;
		}

		[HttpGet]
		public async Task<ActionResult> Index(string returnUrl)
		{
			return View("ChangePassword", await BuildViewModel(returnUrl));
		}

		[HttpGet]
		public ActionResult Edit()
		{
      return View("ChangePasswordSettings", new ViewModels.ChangePassword());
		}

		[HttpPost]
		public async Task<ActionResult> ChangePassword(ViewModels.ChangePassword viewModel)
		{
			User loginUser;
			List<Claim> claims = new();

			if (viewModel.ExistingPasswordBlank)
			{
				ControllerContext.ModelState.Remove(nameof(ViewModels.ChangePassword.Password));
			}

			if (!ControllerContext.ModelState.IsValid)
			{
				return BadRequest(ControllerContext.ModelState);
			}

			if (viewModel.ConfirmPassword != viewModel.NewPassword)
			{
        ModelState.AddModelError<ViewModels.ChangePassword>(model => model.ConfirmPassword, "Your new password and confirm password values do not match.");
        return BadRequest(ModelState);

        //return BadRequest("Your new password and confirm password values do not match");
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
				return Json(new { Title = "Change Password", Message = "You must log in before you can change your password.", Icon = "alert" });				
			}
			else
			{
				if (!loginUser.Approved)
				{
					return Json(new { Title = "Change Password", Message = "Your account has not been approved.", Icon = "warning" });
				}
				else if (!loginUser.Verified)
				{
					return Json(new { Title = "Change Password", Message = "Your account has not been verified.", Icon = "warning" });
				}
				else if (!viewModel.ExistingPasswordBlank && (String.IsNullOrEmpty(viewModel.Password) || !loginUser.Secrets.VerifyPassword(viewModel.Password)))
				{
          ModelState.AddModelError<ViewModels.ChangePassword>(model => model.Password, "Invalid password.");
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
            ModelState.AddModelError<ViewModels.ChangePassword>(model => model.Password, "Invalid password.");
            return BadRequest(modelState);
          }
          else
          {
            this.Logger?.LogInformation("Saving password change for {user}.", loginUser.UserName);
            await this.UserManager.SetPassword(loginUser, viewModel.NewPassword);

            if (!Url.IsLocalUrl(viewModel.ReturnUrl)) viewModel.ReturnUrl = "";
            string location = String.IsNullOrEmpty(viewModel.ReturnUrl) ? Url.Content("~/") : viewModel.ReturnUrl;
            //ControllerContext.HttpContext.Response.Headers.Add("X-Location", location);
            //return StatusCode((int)System.Net.HttpStatusCode.Found);
            return ControllerContext.HttpContext.NucleusRedirect(location);
          }
        }
			}			
		}

		private async Task<ViewModels.ChangePassword> BuildViewModel(string returnUrl)
		{
      User loginUser;
			if (User.IsSystemAdministrator())
			{
				loginUser = await this.UserManager.GetSystemAdministrator(ControllerContext.HttpContext.User.Identity.Name);
			}
			else
			{
				loginUser = await this.UserManager.Get(this.Context.Site, ControllerContext.HttpContext.User.Identity.Name);
			}
      
      return new ViewModels.ChangePassword()
			{
				ExistingPasswordBlank = loginUser != null && String.IsNullOrEmpty(loginUser.Secrets.PasswordHashAlgorithm),
        UserPasswordExpired = User.IsPasswordExpired(),
				ReturnUrl = returnUrl
			};
		}

	}
}
