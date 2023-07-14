using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Nucleus.ViewFeatures;
using Nucleus.Extensions;
using Nucleus.Abstractions.Mail;
using Nucleus.Abstractions.Models.Mail.Template;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nucleus.Modules.Account.Controllers;

[Extension("Account")]
public class LoginController : Controller
{
	private Context Context { get; }
	private ILogger<LoginController> Logger { get; }
	private IUserManager UserManager { get; }
	private ISessionManager SessionManager { get; }
	private IPageManager PageManager { get; }
	private IPageModuleManager PageModuleManager { get; }
	private IMailClientFactory MailClientFactory { get; }
	private ClaimTypeOptions ClaimTypeOptions { get; }				
	private IMailTemplateManager MailTemplateManager { get; }

	public LoginController(Context context, IMailClientFactory mailClientFactory, IMailTemplateManager mailTemplateManager, ILogger<LoginController> logger, IUserManager userManager, ISessionManager sessionManager, IPageManager pageManager, IPageModuleManager pageModuleManager, IOptions<ClaimTypeOptions> claimTypeOptions)
	{
		this.Context = context;
		this.MailClientFactory = mailClientFactory;
		this.MailTemplateManager = mailTemplateManager;
		this.Logger = logger;
		this.UserManager = userManager;
		this.PageManager = pageManager;
		this.PageModuleManager = pageModuleManager;
		this.SessionManager = sessionManager;
		this.ClaimTypeOptions = claimTypeOptions.Value;
	}

	[HttpGet]
	public ActionResult Index(string username, string returnUrl, string token, string reason)
	{
		ViewModels.Login viewModel = BuildViewModel(returnUrl);

		// allow username defaulting
		if (!String.IsNullOrEmpty(username) && String.IsNullOrEmpty(viewModel.Username))
		{
			viewModel.Username = username;
		}

		switch (reason)
		{
			case nameof(System.Net.HttpStatusCode.Forbidden):
				viewModel.Message = "Access was denied.  Your account is not authorized to use this system.";
				break;

      case nameof(System.Net.HttpStatusCode.Found):
        viewModel.Message = "Your settings have been updated.";
        break;

      default:
				viewModel.Message = "";
				break;
		}
		

		if (!String.IsNullOrEmpty(token) && viewModel.AllowPasswordReset)
		{
			return View("ResetPassword", new ViewModels.ResetPassword { UserName = username, PasswordResetToken = token });
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
		viewModel.WriteSettings(this.Context.Module);
		
		this.PageModuleManager.SaveSettings(this.Context.Module);

		return Ok();
	}

	[HttpPost]
	async public Task<ActionResult> Login(ViewModels.Login viewModel)
	{
		User loginUser;
		List<Claim> claims = new();

		viewModel.ReadSettings(this.Context.Module);

		loginUser = await this.UserManager.Get(this.Context.Site, viewModel.Username);

		if (loginUser == null)
		{
			// try system admin
			loginUser = await this.UserManager.GetSystemAdministrator(viewModel.Username);
		}

		if (loginUser == null)
		{
			await Task.Delay(TimeSpan.FromSeconds(10));
			return Json(new { Title = "Login", Message = "Invalid username or password.", Icon = "alert" });
		}
		else
		{
			if (!await this.UserManager.VerifyPassword(loginUser, viewModel.Password))
			{
				await Task.Delay(TimeSpan.FromSeconds(10));
				return Json(new { Title = "Login", Message = "Invalid username or password.", Icon = "alert" });
			}
			else
			{
				if (!loginUser.Approved)
				{
					return Json(new { Title = "Login", Message = "Your account has not been approved.", Icon = "warning" });
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
								return Json(new { Title = "Login", Message = "Your verification token has expired. Contact the system administrator for help.", Icon = "error" });
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
							return Json(new { Title = "Login", Message = "Invalid verification token.", Icon = "error" });
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

	[HttpPost]
	public async Task<ActionResult> ResendVerificationCode(ViewModels.Login viewModel)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		if (this.Context != null && this.Context.Site != null && !String.IsNullOrEmpty(viewModel.Username))
		{
			User user = await this.UserManager.Get(this.Context.Site, viewModel.Username);
			if (user == null)
			{
				Logger.LogWarning("User not found for username '{viewModel.Username}'.", viewModel.Username);
				await Task.Delay(TimeSpan.FromSeconds(10));
        ModelState.AddModelError<ViewModels.Login>(model => model.Username, "Invalid username.");
        return BadRequest(ModelState);
			}
			else
			{
				SiteTemplateSelections templateSelections = this.Context.Site.GetSiteTemplateSelections();

				if (templateSelections.WelcomeNewUserTemplateId.HasValue)
				{
					MailTemplate template = await this.MailTemplateManager.Get(templateSelections.WelcomeNewUserTemplateId.Value);
					string email = user.Profile.GetProperty(System.Security.Claims.ClaimTypes.Email)?.Value;
					if (template != null && email != null)
					{
						await this.UserManager.SetVerificationToken(user);
						await this.UserManager.Save(this.Context.Site, user);
						SitePages sitePages = this.Context.Site.GetSitePages();

						UserMailTemplateData args = new ()
						{
							Site = this.Context.Site,
							User = user.GetCensored(),
							LoginPage = sitePages.LoginPageId.HasValue ? await this.PageManager.Get(sitePages.LoginPageId.Value) : null,
							PrivacyPage = sitePages.PrivacyPageId.HasValue ? await this.PageManager.Get(sitePages.PrivacyPageId.Value) : null,
							TermsPage = sitePages.TermsPageId.HasValue ? await this.PageManager.Get(sitePages.TermsPageId.Value) : null
						};

						Logger.LogTrace("Sending Welcome email '{emailTemplateName}' to user '{userid}'.", template.Name, user.Id);

						using (IMailClient mailClient = this.MailClientFactory.Create(this.Context.Site))
						{
							await mailClient.Send(template, args, email);
						}

						return Json(new { Title = "Re-send Verification Code", Message = "Your verification code has been sent.", Icon = "alert" });

					}
					else
					{
						Logger.LogTrace("Failed sending Welcome email '{emailTemplateName}' to user '{userid}'. Email address is not set or the site welcome new user template is invalid.", template?.Name, user.Id);
						return Json(new { Title = "Re-send Verification Code", Message = "Your verification code has not been sent. Please contact the site administrator for help.", Icon = "warning" });
					}
				}
				else
				{
					Logger.LogTrace("Not sending Welcome email to user '{userid}' because no welcome email is configured for site '{siteId}'.", user.Id, this.Context.Site.Id);
					return Json(new { Title = "Re-send Verification Code", Message = "Your site administrator has not configured a welcome email template.  Please contact the site administrator for help.", Icon = "warning" });
				}
			}
		}
		else
		{
			return BadRequest();
		}

		//return View("Recover", viewModel);
	}


	public async Task<ActionResult> Logout(string returnUrl)
	{
		await this.SessionManager.SignOut(HttpContext);
		
		return Redirect(String.IsNullOrEmpty(returnUrl) ? "~/" : returnUrl);
	}

	private ViewModels.Login BuildViewModel(string returnUrl)
	{
		ViewModels.Login viewModel = new();
		viewModel.ReadSettings(this.Context.Module);

		viewModel.ReturnUrl = returnUrl;
		return viewModel;
	}

}
