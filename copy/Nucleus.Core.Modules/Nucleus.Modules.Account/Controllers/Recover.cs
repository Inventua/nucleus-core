using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Core;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Nucleus.Core.Authentication;
using Nucleus.ViewFeatures;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.Mail;
using Microsoft.AspNetCore.Routing;

namespace Nucleus.Modules.Account.Controllers
{
	[Extension("Account")]
	public class RecoverController : Controller
	{
		private Context Context { get; }
		private ILogger<LoginController> Logger { get; }
		private UserManager UserManager { get; }
		private PageManager PageManager { get; }
		private MailTemplateManager MailTemplateManager { get; }
		private MailClientFactory MailClientFactory { get; }
		private LinkGenerator LinkGenerator { get; }

		public RecoverController(Context context, ILogger<LoginController> Logger, LinkGenerator linkGenerator, UserManager userManager, PageManager pageManager, MailTemplateManager mailTemplateManager, MailClientFactory mailClientFactory)
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

		[HttpPost]
		public ActionResult RecoverUserName(ViewModels.Recover viewModel)
		{
			if (!this.Context.Module.ModuleSettings.Get(LoginController.ModuleSettingsKeys.AllowUsernameRecovery, true))
			{
				return BadRequest();
			}

			if (this.Context != null && this.Context.Site != null && !String.IsNullOrEmpty(viewModel.Email))
			{
				User user = this.UserManager.GetByEmail(this.Context.Site, viewModel.Email);
				if (user == null)
				{
					return BadRequest();
				}
				else
				{
					SiteTemplateSelections templateSelections = this.Context.Site.GetSiteTemplateSelections();

					if (templateSelections.AccountNameReminderTemplateId.HasValue)
					{
						MailTemplate template = this.MailTemplateManager.Get(templateSelections.AccountNameReminderTemplateId.Value);
						if (template != null && viewModel.Email != null)
						{
							MailArgs args = new()
							{
								{ "Site", this.Context.Site },
								{ "User", GetCensoredUser(user) },
								{ "Urls.Login", GetLoginPageUri() }
							};

							Logger.LogTrace("Sending account name reminder email {0} to user {1}.", template.Name, user.Id);

							using (MailClient mailClient = this.MailClientFactory.Create())
							{
								mailClient.Send(template, args, viewModel.Email);
								viewModel.Message = "Account Name Reminder email sent." ;
							}
						}
					}
					else
					{
						Logger.LogTrace("Not sending account name reminder to user {0} because no Account Name Reminder Template is configured for site {1}.", user.Id, this.Context.Site.Id);
						viewModel.Message = "Your site administrator has not configured an Account Name Reminder email template.  Please contact the site administrator for help.";				
					}
				}			
			}
			else
			{
				return BadRequest();
			}

			return View("Recover", viewModel);
		}

		/// <summary>
		/// Return a copy of the supplied user with sensitive data removed.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		private User GetCensoredUser(User user)
		{
			return new User()
			{
				Id = user.Id,
				Profile = user.Profile,
				UserName = user.UserName,
				Secrets = new() { PasswordResetToken = user.Secrets?.PasswordResetToken}
			};
		}

		

		[HttpPost]
		public ActionResult RecoverPassword(ViewModels.Recover viewModel)
		{
			if (!this.Context.Module.ModuleSettings.Get(LoginController.ModuleSettingsKeys.AllowPasswordReset, true))
			{
				return BadRequest();
			}

			if (this.Context != null && this.Context.Site != null && !String.IsNullOrEmpty(viewModel.Email))
			{
				User user = this.UserManager.GetByEmail(this.Context.Site, viewModel.Email);
				if (user == null)
				{
					return BadRequest();
				}
				else
				{
					SiteTemplateSelections templateSelections = this.Context.Site.GetSiteTemplateSelections();

					if (templateSelections.PasswordResetTemplateId.HasValue)
					{
						MailTemplate template = this.MailTemplateManager.Get(templateSelections.PasswordResetTemplateId.Value);
						if (template != null && viewModel.Email != null)
						{
							this.UserManager.SetPasswordResetToken(user);

							MailArgs args = new()
							{
								{ "Site", this.Context.Site },
								{ "User", GetCensoredUser(user) },
								{ "Urls", new Dictionary<string, object> { { "ResetPassword", new System.Uri(GetLoginPageUri(), $"?token={user.Secrets.PasswordResetToken}") } } }
							};

							Logger.LogTrace("Sending password reset email {0} to user {1}.", template.Name, user.Id);

							using (MailClient mailClient = this.MailClientFactory.Create())
							{
								mailClient.Send(template, args, viewModel.Email);
								viewModel.Message = "Password Reset email sent.";
							}
						}
					}
					else
					{
						Logger.LogTrace("Not sending password reset to user {0} because no password reset template is configured for site {1}.", user.Id, this.Context.Site.Id);
						viewModel.Message = "Your site administrator has not configured a password reset email template.  Please contact the site administrator for help.";
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
		public ActionResult ResetPassword(ViewModels.ResetPassword viewModel)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			if (!this.Context.Module.ModuleSettings.Get(LoginController.ModuleSettingsKeys.AllowPasswordReset, true))
			{
				return BadRequest();
			}

			Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = this.UserManager.ValidatePasswordComplexity(viewModel.NewPassword);
			if (!modelState.IsValid)
			{
				return BadRequest(modelState);
			}

			User user = this.UserManager.Get(this.Context.Site, viewModel.UserName);
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

			// invalidate the "used up" password reset token
			user.Secrets.PasswordResetToken = "";
			user.Secrets.PasswordResetTokenExpiryDate = DateTime.MinValue;

			this.UserManager.Save(this.Context.Site, user);

			ControllerContext.HttpContext.Response.Headers.Add("X-Location", GetLoginPageUri().ToString());
			return StatusCode((int)System.Net.HttpStatusCode.Found);
		}

		// todo: move this to a generic location?
		private System.Uri GetLoginPageUri()
		{
			PageRoute loginPageRoute;
			Page loginPage = this.PageManager.Get(this.Context.Site.GetSitePages().LoginPageId.Value);
			if (loginPage != null)
			{
				loginPageRoute = loginPage.DefaultPageRoute();
				return Url.GetAbsoluteUri(loginPageRoute.Path);
			}
			else
			{
				RouteValueDictionary routeDictionary = new RouteValueDictionary();

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
				AllowPasswordReset = this.Context.Module.ModuleSettings.Get(LoginController.ModuleSettingsKeys.AllowPasswordReset, true),
				AllowUsernameRecovery = this.Context.Module.ModuleSettings.Get(LoginController.ModuleSettingsKeys.AllowUsernameRecovery, true),
				ReturnUrl = returnUrl
			};
		}

	}
}
