using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.Account.Controllers;

[Extension("Account")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class SignupAdminController : Controller
{
  private Context Context { get; }
  private IPageModuleManager PageModuleManager { get; }

  public SignupAdminController(Context Context, IPageModuleManager pageModuleManager)
  {
    this.Context = Context;
    this.PageModuleManager = pageModuleManager;
  }

  [HttpGet]
  [HttpPost]
  public ActionResult Settings(ViewModels.SignupSettings viewModel)
  {
    return View("SignupSettings", BuildSettingsViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> SaveSettings(ViewModels.SignupSettings viewModel)
  {
    if (viewModel.RecaptchaEnabled)
    {
      if (String.IsNullOrEmpty(viewModel.RecaptchaSecretKey))
      {
        viewModel.SetSecretKey(this.Context.Site, "");
      }
      else if (viewModel.RecaptchaSecretKey != ViewModels.SignupSettings.DUMMY_PASSWORD)
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
      // Repopulate the recaptcha settings
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


  private ViewModels.SignupSettings BuildSettingsViewModel(ViewModels.SignupSettings viewModel)
  {
    viewModel ??= new();

    viewModel.ReadSettings(this.Context.Module);

    return viewModel;
  }
}

