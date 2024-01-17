using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Extensions;
using Nucleus.ViewFeatures;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.Mail;
using Microsoft.AspNetCore.Routing;
using Nucleus.Abstractions.Mail;

namespace Nucleus.Modules.Account.Controllers
{
	[Extension("Account")]
	public class RecoverController : Controller
	{
		private Context Context { get; }
		private ILogger<LoginController> Logger { get; }
		private IUserManager UserManager { get; }
		private IPageManager PageManager { get; }
		private IMailTemplateManager MailTemplateManager { get; }
		private IMailClientFactory MailClientFactory { get; }
		private LinkGenerator LinkGenerator { get; }

		public RecoverController(Context context, ILogger<LoginController> Logger, LinkGenerator linkGenerator, IUserManager userManager, IPageManager pageManager, IMailTemplateManager mailTemplateManager, IMailClientFactory mailClientFactory)
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
		public ActionResult Index(string returnUrl)
		{
			return View("Recover", BuildViewModel(returnUrl));
		}

		[HttpGet]
		public ActionResult Edit(string returnUrl)
		{
      return View("RecoverSettings", new ViewModels.Signup() { ReturnUrl = returnUrl });
		}

		[HttpPost]
		public async Task<ActionResult> RecoverUserName(ViewModels.Recover viewModel)
		{
			if (!this.Context.Module.ModuleSettings.Get(Models.Settings.ModuleSettingsKeys.AllowUsernameRecovery, true))
			{
				return BadRequest();
			}

			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			if (this.Context != null && this.Context.Site != null && !String.IsNullOrEmpty(viewModel.Email))
			{
				User user = await this.UserManager.GetByEmail(this.Context.Site, viewModel.Email);
				if (user == null)
				{
					Logger.LogWarning("User not found for email {viewModel.Email}.", viewModel.Email);
					
          // we return a message saying that we have sent a reminder (even though we have not), so that this function can't be used to determine whether
          // an email address belongs to a user in the system
          return Json(new { Title = "Recover Account", Message = "An account name reminder email has been sent.", Icon = "alert" });
        }
				else
				{
					if (!user.Approved)
					{
						return Json(new { Title = "Recover Account", Message = "Your account has not been approved.", Icon = "error" });
					}
					else if (!user.Verified)
					{
						return Json(new { Title = "Recover Account", Message = "Your account has not been verified.", Icon = "error" });
					}
					else
					{
						SiteTemplateSelections templateSelections = this.Context.Site.GetSiteTemplateSelections();

						if (templateSelections.AccountNameReminderTemplateId.HasValue)
						{
							MailTemplate template = await this.MailTemplateManager.Get(templateSelections.AccountNameReminderTemplateId.Value);
							if (template != null && viewModel.Email != null)
							{
                SitePages sitePages = this.Context.Site.GetSitePages();

                //Models.Mail.RecoveryEmailModel args = new()
                //{
                //	Site = this.Context.Site ,
                //	User = user.GetCensored() ,
                //	Url = GetLoginPageUri().ToString()
                //};
                Abstractions.Models.Mail.Template.UserMailTemplateData args = new()
                {
                  Site = this.Context.Site,
                  User = user.GetCensored(),
                  Url = GetLoginPageUri().ToString(),
                  LoginPage = sitePages.LoginPageId.HasValue ? await this.PageManager.Get(sitePages.LoginPageId.Value) : null,
                  PrivacyPage = sitePages.PrivacyPageId.HasValue ? await this.PageManager.Get(sitePages.PrivacyPageId.Value) : null,
                  TermsPage = sitePages.TermsPageId.HasValue ? await this.PageManager.Get(sitePages.TermsPageId.Value) : null
                };

                Logger.LogTrace("Sending account name reminder email '{templateName}' to user '{userId}', email '{email}'.", template.Name, user.Id, viewModel.Email);

								using (IMailClient mailClient = this.MailClientFactory.Create(this.Context.Site))
								{
									await mailClient.Send(template, args, viewModel.Email);
									return Json(new { Title = "Recover Account", Message = "An account name reminder email has been sent.", Icon = "alert" });
								}
							}
						}

						else
						{
							Logger.LogTrace("Not sending account name reminder to user {userId} because no Account Name Reminder Template is configured for site {siteId}.", user.Id, this.Context.Site.Id);
							return Json(new { Title = "Recover Account", Message = "Your site administrator has not configured an Account Name Reminder email template.  Please contact the site administrator for help.", Icon = "error" });
						}
					}
				}			
			}
			else
			{
				return BadRequest();
			}

			return View("Recover", viewModel);
		}

		

		[HttpPost]
		public async Task<ActionResult> RecoverPassword(ViewModels.Recover viewModel)
		{
			if (!this.Context.Module.ModuleSettings.Get(Models.Settings.ModuleSettingsKeys.AllowPasswordReset, true))
			{
				return BadRequest();
			}

			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			if (this.Context != null && this.Context.Site != null && !String.IsNullOrEmpty(viewModel.Email))
			{
				User user = await this.UserManager.GetByEmail(this.Context.Site, viewModel.Email);
				if (user == null)
				{
					Logger.LogWarning("User not found for email {viewModel.Email}.", viewModel.Email);
          // we return a message saying that we have sent a password reset email (even though we have not), so that this function can't be used to determine whether
          // an email address belongs to a user in the system
          return Json(new { Title = "Password Reset", Message = "Password Reset email sent.", Icon = "alert" });
        }
				else
				{
					SiteTemplateSelections templateSelections = this.Context.Site.GetSiteTemplateSelections();

					if (templateSelections.PasswordResetTemplateId.HasValue)
					{
						MailTemplate template = await this.MailTemplateManager.Get(templateSelections.PasswordResetTemplateId.Value);
						if (template != null && viewModel.Email != null)
						{
							await this.UserManager.SetPasswordResetToken(user);

              ////Models.Mail.RecoveryEmailModel args = new()
              ////{
              ////	Site = this.Context.Site ,
              ////	User = user.GetCensored(),
              ////	Url = new System.Uri(await GetLoginPageUri(), $"?token={user.Secrets.PasswordResetToken}").ToString()
              ////};
              SitePages sitePages = this.Context.Site.GetSitePages();

              Abstractions.Models.Mail.Template.UserMailTemplateData args = new()
              {
                Site = this.Context.Site,
                User = user.GetCensored(),    
                Url = new System.Uri(await GetLoginPageUri(), $"?token={user.Secrets.PasswordResetToken}").ToString(),
                LoginPage = sitePages.LoginPageId.HasValue ? await this.PageManager.Get(sitePages.LoginPageId.Value) : null,
                PrivacyPage = sitePages.PrivacyPageId.HasValue ? await this.PageManager.Get(sitePages.PrivacyPageId.Value) : null,
                TermsPage = sitePages.TermsPageId.HasValue ? await this.PageManager.Get(sitePages.TermsPageId.Value) : null
              };

              Logger.LogTrace("Sending password reset email {templateName} to user {userId}.", template.Name, user.Id);

							using (IMailClient mailClient = this.MailClientFactory.Create(this.Context.Site))
							{
								await mailClient.Send(template, args, viewModel.Email);
								return Json(new { Title = "Password Reset", Message = "Password Reset email sent.", Icon = "alert" });
							}
						}
					}
					else
					{
						Logger.LogTrace("Not sending password reset to user {userId} because no password reset template is configured for site {siteId}.", user.Id, this.Context.Site.Id);
						return Json(new { Title = "Password Reset", Message = "Your site administrator has not configured a password reset email template.  Please contact the site administrator for help.", Icon = "error" });
					}
				}
			}
			else
			{
				return BadRequest();
			}

			return View("Recover", viewModel);
		}
				
		[HttpPost]
		public async Task<ActionResult> ResetPassword(ViewModels.ResetPassword viewModel)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}
			
			if (!this.Context.Module.ModuleSettings.Get(Models.Settings.ModuleSettingsKeys.AllowPasswordReset, true))
			{
				return BadRequest();
			}

			Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = await this.UserManager.ValidatePasswordComplexity(nameof(viewModel.NewPassword), viewModel.NewPassword);
			if (!modelState.IsValid)
			{
				return BadRequest(modelState);
			}

			User user = await this.UserManager.Get(this.Context.Site, viewModel.UserName);
			if (user == null)
			{
				return BadRequest("Your user name or password reset token is invalid.");
			}

			if (user.Secrets.PasswordResetToken != viewModel.PasswordResetToken || user.Secrets.PasswordResetTokenExpiryDate < DateTime.UtcNow)
			{
				return BadRequest("Your user name or password reset token is invalid.");
			}

			// inputs validated, reset the password and navigate to the login page
			user.Secrets.SetPassword(viewModel.NewPassword);

			// invalidate the "consumed" password reset token
			user.Secrets.PasswordResetToken = null;
			user.Secrets.PasswordResetTokenExpiryDate = null;

			await this.UserManager.SaveSecrets(user);
      string uri = (await GetLoginPageUri()).ToString();
      string location = $"{uri}{(uri.Contains('?') ? "&" : "?")}username={user.UserName}&reason={nameof(System.Net.HttpStatusCode.Found)}";
			//ControllerContext.HttpContext.Response.Headers.Add("X-Location", $"{uri}{(uri.Contains('?') ? "&" : "?")}username={user.UserName}&reason={nameof(System.Net.HttpStatusCode.Found)}");
			//return StatusCode((int)System.Net.HttpStatusCode.Found);
      return ControllerContext.HttpContext.NucleusRedirect(location);
    }

		private async Task<System.Uri> GetLoginPageUri()
		{
			PageRoute loginPageRoute;
			Page loginPage = await this.PageManager.Get(this.Context.Site.GetSitePages().LoginPageId.Value);
			if (loginPage != null)
			{
				loginPageRoute = loginPage.DefaultPageRoute();
				return Url.GetAbsoluteUri(loginPageRoute.Path);
			}
			else
			{
				RouteValueDictionary routeDictionary = new();

				routeDictionary.Add("area", "User");
				routeDictionary.Add("controller", "Account");
				routeDictionary.Add("action", "Index");

				return Url.GetAbsoluteUri(this.LinkGenerator.GetPathByRouteValues("Admin", routeDictionary, this.ControllerContext.HttpContext.Request.PathBase, FragmentString.Empty, null));
			}
		}

		private ViewModels.Recover BuildViewModel(string returnUrl)
		{
      return new ViewModels.Recover()
			{
				AllowPasswordReset = this.Context.Module.ModuleSettings.Get(Models.Settings.ModuleSettingsKeys.AllowPasswordReset, true),
				AllowUsernameRecovery = this.Context.Module.ModuleSettings.Get(Models.Settings.ModuleSettingsKeys.AllowUsernameRecovery, true)
			};
		}

	}
}
