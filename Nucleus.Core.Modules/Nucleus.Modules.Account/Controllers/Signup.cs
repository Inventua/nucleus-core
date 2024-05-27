using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nucleus.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;

namespace Nucleus.Modules.Account.Controllers;

[Extension("Account")]
public class SignupController : Controller
{
  private Context Context { get; }
  private ILogger<LoginController> Logger { get; }
  private IUserManager UserManager { get; }
  private ClaimTypeOptions ClaimTypeOptions { get; }
  private ISessionManager SessionManager { get; }
  private IHttpClientFactory HttpClientFactory { get; }

  public SignupController(Context context, ILogger<LoginController> Logger, IUserManager userManager, IHttpClientFactory httpClientFactory, ISessionManager sessionManager, IOptions<ClaimTypeOptions> claimTypeOptions)
  {
    this.Context = context;
    this.Logger = Logger;
    this.UserManager = userManager;
    this.HttpClientFactory = httpClientFactory;
    this.SessionManager = sessionManager;
    this.ClaimTypeOptions = claimTypeOptions.Value;
  }

  [HttpGet]
  public async Task<ActionResult> Index(string returnUrl)
  {
    if (this.Context.Site.UserRegistrationOptions.HasFlag(Site.SiteUserRegistrationOptions.SignupAllowed))
    {
      return View("Signup", await BuildViewModel(returnUrl));
    }
    else
    {
      return Forbid();
    }
  }

  [HttpPost]
  public async Task<ActionResult> Signup(ViewModels.Signup viewModel)
  {
    if (!this.ModelState.IsValid)
    {
      return BadRequest(this.ModelState);
    }

    Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState = await this.UserManager.ValidatePasswordComplexity(nameof(viewModel.NewPassword), viewModel.NewPassword);

    if (viewModel.ConfirmPassword != viewModel.NewPassword)
    {
      modelState.AddModelError<ViewModels.Signup>(model => model.ConfirmPassword, "Your new password and confirm password values do not match.");
    }

    if (!modelState.IsValid)
    {
      return BadRequest(modelState);
    }

    Models.SignupSettings settings = new();
    settings.ReadSettings(this.Context.Module);

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

        Models.SiteVerifyResponseToken responseToken = await googleRecaptcha.VerifyToken(viewModel.RecaptchaVerificationToken);

        if (!responseToken.Success)
        {
          return Forbid();  // returns to the home page
        }
       }
      catch (Exception ex)
      {
        this.Logger?.LogError(ex, "Verify token.");
        return Forbid();
      }
    }

    try
    {
      User newUser = await this.UserManager.CreateNew(this.Context.Site);

      newUser.UserName = viewModel.User.UserName;
      newUser.Profile = viewModel.User.Profile;
      newUser.Secrets = new();
      newUser.Secrets.SetPassword(viewModel.NewPassword);

      if (!newUser.Verified)
      {
        await this.UserManager.SetVerificationToken(newUser);
      }

      await this.UserManager.Save(this.Context.Site, newUser);

      if (!Url.IsLocalUrl(viewModel.ReturnUrl)) viewModel.ReturnUrl = "";

      if (newUser.Approved && newUser.Verified)
      {
        UserSession session = await this.SessionManager.CreateNew(this.Context.Site, newUser, false, ControllerContext.HttpContext.Connection.RemoteIpAddress);
        await this.SessionManager.SignIn(session, HttpContext, viewModel.ReturnUrl);
      }

      string location = String.IsNullOrEmpty(viewModel.ReturnUrl) ? Url.Content("~/") : viewModel.ReturnUrl;
      //ControllerContext.HttpContext.Response.Headers.Add("X-Location", location);
      //return StatusCode((int)System.Net.HttpStatusCode.Found);
      return ControllerContext.HttpContext.NucleusRedirect(location);
    }
    catch (Exception ex)
    {
      if (ex.Message.Contains("Please enter a unique name", StringComparison.OrdinalIgnoreCase) || ex.InnerException?.Message.Contains("unique constraint failed", StringComparison.OrdinalIgnoreCase) == true)
      {
        this.ModelState.AddModelError<ViewModels.Signup>(model => model.User.UserName, "User name is already in use.");
        return BadRequest(this.ModelState);
      }
      else
      {
        throw;
      }
    }
  }


 

  private async Task<ViewModels.Signup> BuildViewModel(string returnUrl)
  {
    if (!Url.IsLocalUrl(returnUrl)) returnUrl = "";

    //return new ViewModels.Signup()
    ViewModels.Signup viewModel = new()
    {
      ShowForm = true,
      ReturnUrl = returnUrl,
      User = await this.UserManager.CreateNew(this.Context.Site),
      ClaimTypeOptions = this.ClaimTypeOptions
    };

    viewModel.ReadSettings(this.Context.Module);

    return viewModel;
  }

}
