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
    if (!ModelState.IsValid)
    {
      return BadRequest(ModelState);
    }


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

      if (!String.IsNullOrEmpty(viewModel.RecaptchaSiteKey))
      {
        Validate(viewModel.RecaptchaSecretKey, nameof(viewModel.RecaptchaSecretKey), "Enter your Google reCAPTCHA secret key.");
        Validate(viewModel.RecaptchaAction, nameof(viewModel.RecaptchaAction), "Enter the reCAPTCHA action.");

        if (!ControllerContext.ModelState.IsValid)
        {
          return BadRequest(ControllerContext.ModelState);
        }
      }
    }
    else
    {
      // Repopulate the recaptcha settings
      viewModel.ReadRecaptchaSettings(this.Context.Module);
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

