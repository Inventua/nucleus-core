using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Mail;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using System.Collections.Generic;

namespace Nucleus.Modules.ContactUs.Controllers;

[Extension("ContactUs")]
public class ContactUsController : Controller
{
	private Context Context { get; }

	private ILogger<ContactUsController> Logger { get; }

	private IPageModuleManager PageModuleManager { get; }

	private IListManager ListManager { get; }

	private IMailTemplateManager MailTemplateManager { get; }

	private IMailClientFactory MailClientFactory { get; }

	private IHttpClientFactory HttpClientFactory { get; }

	private const string GOOGLE_RECAPTCHA_VERIFY_SITE = "https://www.google.com/recaptcha/api/siteverify";

	public ContactUsController(Context context, IHttpClientFactory httpClientFactory, IListManager listManager, IPageModuleManager pageModuleManager, IMailTemplateManager mailTemplateManager, IMailClientFactory mailClientFactory, ILogger<ContactUsController> logger)
	{
		this.Context = context;
		this.HttpClientFactory = httpClientFactory;
		this.ListManager = listManager;
		this.PageModuleManager = pageModuleManager;
		this.MailTemplateManager = mailTemplateManager;
		this.MailClientFactory = mailClientFactory;
		this.Logger = logger;
	}

	[HttpGet]
	public async Task<ActionResult> Index()
	{
		return View("Viewer", await BuildViewModel());
	}

	private void Validate(Boolean isRequired, string value, string propertyName, string message)
	{
		if (isRequired && String.IsNullOrEmpty(value))
		{
			ModelState.AddModelError(propertyName, message);
		}
	}

	private void Validate(Boolean isRequired, ListItem value, string propertyName, string message)
	{
		if (isRequired && (value == null || String.IsNullOrEmpty(value.Value) || value.Id == Guid.Empty))
		{
			ModelState.AddModelError(propertyName + ".Id", message);
		}
	}

	private string MessagePropertyName(string propertyName)
	{
		return $"{nameof(ViewModels.Viewer.Message)}.{propertyName}";
	}

	private void Validate(ViewModels.Viewer viewModel, Models.Settings settings)
	{
		Validate(settings.RequireName, viewModel.Message.FirstName, MessagePropertyName(nameof(viewModel.Message.FirstName)), "Enter your first name.");
		Validate(settings.RequireName, viewModel.Message.LastName, MessagePropertyName(nameof(viewModel.Message.LastName)), "Enter your last name.");
		Validate(settings.RequirePhoneNumber, viewModel.Message.PhoneNumber, MessagePropertyName(nameof(viewModel.Message.PhoneNumber)), "Enter your phone number.");
		Validate(settings.RequireCompany, viewModel.Message.Company, MessagePropertyName(nameof(viewModel.Message.Company)), "Enter your company name.");

		Validate(settings.RequireCategory, viewModel.Message.Category, MessagePropertyName(nameof(viewModel.Message.Category)), "Select a message category.");
		Validate(settings.RequireSubject, viewModel.Message.Subject, MessagePropertyName(nameof(viewModel.Message.Subject)), "Enter the subject of your message.");
  }

  [HttpPost]
	public async Task<ActionResult> Send(ViewModels.Viewer viewModel)
	{
    Models.SiteVerifyResponseToken responseToken = null;

    // Remove automatic validation for category because we have custom validation in .Validate()
    ControllerContext.ModelState.RemoveAll<ViewModels.Viewer>(model => model.Message.Category);

    // Remove the required validation for recipients and mail template as they are validation for settings page.
    ControllerContext.ModelState.Remove<ViewModels.Viewer>(model => model.SendTo);
    ControllerContext.ModelState.Remove<ViewModels.Viewer>(model => model.MailTemplateId);
    ControllerContext.ModelState.Remove<ViewModels.Viewer>(model => model.CategoryListId);

    Models.Settings settings = new();
		settings.ReadSettings(this.Context.Module);
    Validate(viewModel, settings);

		if (!ControllerContext.ModelState.IsValid)
		{
			return BadRequest(ControllerContext.ModelState);
		}

    if (settings.RecaptchaEnabled)
    {
      try
      {
        GoogleRecaptchaHandler googleRecaptcha = new(
          this.HttpClientFactory,
          this.Logger,
          settings.GetSecretKey(this.Context.Site),
          settings.RecaptchaAction,
          settings.RecaptchaScoreThreshold,
          this.HttpContext.Connection.RemoteIpAddress.ToString()
        );

        responseToken = await googleRecaptcha.VerifyToken(viewModel.RecaptchaVerificationToken);
        
        if (!responseToken.Success)
        {
          // pretend it worked
          viewModel.MessageSent = true;
          return View("Viewer", viewModel); 
        }
      }
      catch (Exception ex)
      {
        this.Logger?.LogError(ex, "Verifying reCAPTCHA token.");
        viewModel.MessageSent = true;
        return View("Viewer", viewModel);
      }
    }

    if (this.Context != null && this.Context.Site != null)
		{
      if (viewModel.Message.Category.Id != Guid.Empty)
      {
        viewModel.Message.Category = await this.ListManager.GetListItem(viewModel.Message.Category.Id);
      }

      // send contact email (if recipients and mail template have been set)
      if (!String.IsNullOrEmpty(settings.SendTo))
			{
				if (settings.MailTemplateId != null && settings.MailTemplateId != Guid.Empty)
				{
					MailTemplate template = await this.MailTemplateManager.Get(settings.MailTemplateId.Value);
					if (template != null)
					{
            Models.Mail.TemplateModel args = new()
            {
              Site = this.Context.Site,
              Message = viewModel.Message,
              UserVerificationScore = responseToken?.Score ?? 0,
							Settings = GetCensored(settings)
						};

						Logger.LogTrace("Sending contact email '{emailTemplateName}' to '{sendTo}'.", template.Name, settings.SendTo);

						try
						{
							using (IMailClient mailClient = this.MailClientFactory.Create(this.Context.Site))
							{
								await mailClient.Send(template, args, settings.SendTo);
							}
						}
						catch (Exception ex)
						{
							Logger.LogError(ex, "Error sending contact email '{emailTemplateName}' to '{sendTo}'.", template.Name, settings.SendTo);
							return Problem("There was an error sending the contact email.", null, (int)System.Net.HttpStatusCode.InternalServerError, "Error Sending Email");
						}
					}
				}
				else
				{
					Logger.LogTrace("Not sending contact email to '{sendTo}' because no template has been set.", settings.SendTo);
				}
			}
			else
			{
				Logger.LogTrace("Not sending contact email to '{sendTo}' because the module does not have a send to value set.", settings.SendTo);
			}
		}

		viewModel.MessageSent = true;

		return View("Viewer", viewModel);
	}

	/// <summary>
	/// Return a copy of the supplied user with sensitive data (role memberships, and most secrets) removed.
	/// </summary>
	/// <param name="user"></param>
	/// <returns></returns>
	public Models.Settings GetCensored(Models.Settings settings)
	{
		return new Models.Settings()
		{
			MailTemplateId = settings.MailTemplateId,
      RecaptchaEnabled = settings.RecaptchaEnabled,
			RecaptchaAction = settings.RecaptchaAction,
			RequireCategory = settings.RequireCategory,
			RequireCompany = settings.RequireCompany,
			RequireName = settings.RequireName,
			RequirePhoneNumber = settings.RequirePhoneNumber,
			RequireSubject = settings.RequireSubject,
			ShowCategory = settings.ShowCategory,
			ShowCompany = settings.ShowCompany,
			ShowName = settings.ShowName,
			ShowPhoneNumber = settings.ShowPhoneNumber,
			ShowSubject = settings.ShowSubject,
			SendTo = settings.SendTo
		};
	}

	private async Task<ViewModels.Viewer> BuildViewModel()
	{
		ViewModels.Viewer viewModel = new()
		{
			IsAdmin = User.IsSiteAdmin(this.Context.Site)
		};

		viewModel.ReadSettings(this.Context.Module);

    if (viewModel.CategoryListId != null && viewModel.CategoryListId != Guid.Empty)
    {
      viewModel.CategoryList = await this.ListManager.Get(viewModel.CategoryListId.Value);
    }

    //Test if a mail provider has been selected 
    try
    {
      IMailClient mailClient = this.MailClientFactory.Create(this.Context.Site);
      viewModel.IsMailConfigured = true;
    }
    catch (InvalidOperationException) 
    {
      viewModel.IsMailConfigured = false;
    }

      // default values if user is logged on
    if (User.Identity.IsAuthenticated)
		{
			viewModel.Message.FirstName = User.GetUserClaim<String>(System.Security.Claims.ClaimTypes.GivenName);
			viewModel.Message.LastName = User.GetUserClaim<String>(System.Security.Claims.ClaimTypes.Surname);
			viewModel.Message.Email = User.GetUserClaim<String>(System.Security.Claims.ClaimTypes.Email);
			viewModel.Message.PhoneNumber = User.GetUserClaim<String>(System.Security.Claims.ClaimTypes.OtherPhone);
		}

		return viewModel;
	}
}