using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.ContactUs.Controllers;

[Extension("ContactUs")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class ContactUsAdminController : Controller
{
	private Context Context { get; }
	private IPageModuleManager PageModuleManager { get; }
	private IContentManager ContentManager { get; }
	private IListManager ListManager { get; }
	private IMailTemplateManager MailTemplateManager { get; }

	public ContactUsAdminController(Context Context, IPageModuleManager pageModuleManager, IContentManager contentManager, IListManager listManager, IMailTemplateManager mailTemplateManager)
	{
		this.Context = Context;
		this.PageModuleManager = pageModuleManager;
		this.ContentManager = contentManager;
		this.ListManager = listManager;
		this.MailTemplateManager = mailTemplateManager;
	}

	[HttpGet]
	[HttpPost]
	public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
	{
		return View("Settings", await BuildSettingsViewModel(viewModel));
	}

  [HttpPost]
	public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
	{
    if (!ModelState.IsValid)
    {
      return BadRequest(ModelState);
    }

    if (String.IsNullOrEmpty(viewModel.RecaptchaSecretKey))
    {
      viewModel.SetSecretKey(this.Context.Site, "");
    }
    else if (viewModel.RecaptchaSecretKey != ViewModels.Settings.DUMMY_PASSWORD)
		{
			viewModel.SetSecretKey(this.Context.Site, viewModel.RecaptchaSecretKey);
		}
    else
    {
      viewModel.ReadEncryptedKeys(this.Context.Module);
    }

    if (!String.IsNullOrEmpty(viewModel.RecaptchaSiteKey))
    {
      Validate(viewModel.RecaptchaSecretKey, nameof(viewModel.RecaptchaSecretKey), "Enter your Google reCAPTCHA secret key.");
      Validate(viewModel.RecaptchaAction, nameof(viewModel.RecaptchaAction), "Enter the reCAPTCHA action.");

      if (!ControllerContext.ModelState.IsValid)
      {
        return BadRequest(ControllerContext.ModelState);
      }
    }

    viewModel.SetSettings(this.Context.Site, this.Context.Module);

		await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

		return Ok();
	}

  private void Validate(string value, string propertyName, string message)
  {
    if (String.IsNullOrEmpty(value))
    {
      ModelState.AddModelError(propertyName, message);
    }
  }


  private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
	{
		if (viewModel == null)
		{
			viewModel = new();
		}

		viewModel.ReadSettings(this.Context.Module);

		viewModel.Lists = await this.ListManager.List(this.Context.Site);
    viewModel.MailTemplates = await this.MailTemplateManager.List(this.Context.Site, typeof(Models.Mail.TemplateModel));

		return viewModel;
	}
}

