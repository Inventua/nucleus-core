using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
    if (!viewModel.ShowCategory)
    {
      ModelState.Remove<ViewModels.Settings>(model => model.CategoryListId);
    }

    if (viewModel.RecaptchaEnabled)
    {
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

      if (viewModel.RecaptchaEnabled)
      {
        Validate(viewModel.RecaptchaSiteKey, nameof(viewModel.RecaptchaSiteKey), "reCAPTCHA site key is required.");
        Validate(viewModel.RecaptchaSecretKey, nameof(viewModel.RecaptchaSecretKey), "reCAPTCHA secret key is required.");
        Validate(viewModel.RecaptchaAction, nameof(viewModel.RecaptchaAction), "reCAPTCHA action is required.");
      }
    }
    else
    {
      // Repopulate the viewmodel for recaptcha
      viewModel.ReadRecaptchaSettings(this.Context.Module);
    }

    if (!ModelState.IsValid)
    {
      return BadRequest(ModelState);
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

