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

  private const string GOOGLE_RECAPTCHA_VERIFY_SITE = "https://www.google.com/recaptcha/api/siteverify";

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

    try
    {
      Models.SignupSettings settings = new();
      settings.ReadSettings(this.Context.Module);

      Models.SiteVerifyResponseToken responseToken = await CaptchaVerify(viewModel);

      if (!responseToken.Success)
      {
        if (responseToken.ErrorCodes.Count > 0)
        {
          this.Logger.LogWarning("Unsuccessful reCAPTCHA verification.  Errors: '{errorCodes}'.", String.Join(',', responseToken.ErrorCodes));
        }
        return BadRequest("Unable to send message. Please contact the system administrator.");
      }

      if (responseToken.Action != settings.RecaptchaAction)
      {
        this.Logger.LogWarning("Unexpected reCAPTCHA action in response.  Expected: '{expectedAction}', received: '{receivedAction}'.", settings.RecaptchaAction, responseToken.Action);
        return BadRequest("Unable to send message. Please contact the system administrator.");
      }

      if (responseToken.Score < Convert.ToSingle(settings.RecaptchaScoreThreshold))
      {
        this.Logger.LogWarning("Suspected robot from {address}. Recaptcha verify score was {score}. Action: {action}.", HttpContext.Connection.RemoteIpAddress, responseToken.Score, responseToken.Action);
                
        //return View("Signup", await BuildViewModel(""));
      }
    }
    catch (Exception ex)
    {
      this.Logger.LogError(ex, "Verifying reCAPTCHA token.");
      return BadRequest();
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


  private async Task<Models.SiteVerifyResponseToken> CaptchaVerify(ViewModels.Signup viewModel)
  {
    Models.SignupSettings signUpSettings = new();
    signUpSettings.ReadSettings(this.Context.Module);
    string key = signUpSettings.GetSecretKey(this.Context.Site);

    if (String.IsNullOrEmpty(signUpSettings.RecaptchaSiteKey) || String.IsNullOrEmpty(key))
    {
      // Recaptcha is not enabled, return true to allow form submission.
      return new()
      {
        Success = true,
        Score = 1.0f,
        Action = signUpSettings.RecaptchaAction
      };
    }

    string response = await VerifyRecaptchaToken(key, viewModel.RecaptchaVerificationToken);
    Models.SiteVerifyResponseToken responseToken = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.SiteVerifyResponseToken>(response);

    return responseToken;
  }


  /// <summary>
  /// Verify the user response token.
  /// </summary>
  /// <param name="secretKey"></param>
  /// <param name="recaptchaToken"></param>
  /// <returns></returns>
  private async Task<string> VerifyRecaptchaToken(string secretKey, string recaptchaToken)
  {
    HttpResponseMessage responseMessage;

    using (HttpClient httpClient = this.HttpClientFactory.CreateClient())
    {
      responseMessage = await httpClient.PostAsync($"{GOOGLE_RECAPTCHA_VERIFY_SITE}",
        new FormUrlEncodedContent(new[]
        {
          new KeyValuePair<string, string>("secret", secretKey),
          new KeyValuePair<string, string>("response", recaptchaToken),
          new KeyValuePair<string, string>("remoteip", this.HttpContext.Connection.RemoteIpAddress.ToString())
        })
      );
    }

    responseMessage.EnsureSuccessStatusCode();
    return await responseMessage.Content.ReadAsStringAsync();
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
