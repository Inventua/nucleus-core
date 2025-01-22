using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Nucleus.ViewFeatures;

namespace Nucleus.Modules.Account.Controllers;

[Extension("Account")]
public class UserProfileController : Controller
{
  private Context Context { get; }
  private ILogger<LoginController> Logger { get; }
  private IUserManager UserManager { get; }
  private ISessionManager SessionManager { get; }
  private IPageManager PageManager { get; }
  private ClaimTypeOptions ClaimTypeOptions { get; }

  public UserProfileController(Context context, ILogger<LoginController> Logger, IUserManager userManager, ISessionManager sessionManager, IPageManager pageManager, IOptions<ClaimTypeOptions> claimTypeOptions)
  {
    this.Context = context;
    this.Logger = Logger;
    this.UserManager = userManager;
    this.SessionManager = sessionManager;
    this.PageManager = pageManager;
    this.ClaimTypeOptions = claimTypeOptions.Value;
  }

  [HttpGet]
  public async Task<ActionResult> Index(string returnUrl)
  {
    return View("UserProfile", new ViewModels.UserProfile()
    {
      User = await this.UserManager.Get(this.Context.Site, this.ControllerContext.HttpContext.User.GetUserId()),
      ClaimTypeOptions = this.ClaimTypeOptions,
      ReturnUrl = returnUrl
    });
  }

  [HttpGet]
  public ActionResult Edit()
  {
    return View("UserProfileSettings", new ViewModels.UserProfile());
  }

  [HttpPost]
  public async Task<ActionResult> SaveAccountSettings(ViewModels.UserProfile viewModel)
  {
    this.ControllerContext.ModelState.Remove("User.UserName");

    if (!ControllerContext.ModelState.IsValid)
    {
      return BadRequest(ControllerContext.ModelState);
    }

    if (viewModel.User.Id != ControllerContext.HttpContext.User.GetUserId())
    {
      return BadRequest();
    }

    User existing = await this.UserManager.Get(this.Context.Site, HttpContext.User.GetUserId());
    existing.Profile = viewModel.User.Profile;    

    if (existing.IsSystemAdministrator)
    {
      await this.UserManager.SaveSystemAdministrator(existing);
    }
    else
    {
      await this.UserManager.Save(this.Context.Site, existing);
    }


    if (!Url.IsLocalUrl(viewModel.ReturnUrl)) viewModel.ReturnUrl = "";
    string location = String.IsNullOrEmpty(viewModel.ReturnUrl) ? Url.Content("~/") : viewModel.ReturnUrl;
    //ControllerContext.HttpContext.Response.Headers.Add("X-Location", location);
    //return StatusCode((int)System.Net.HttpStatusCode.Found);
    return ControllerContext.HttpContext.NucleusRedirect(location);
  }

  [HttpPost]
  public async Task<ActionResult> SetupAuthApp(ViewModels.UserProfile viewModel)
  {
    ModelState.Clear();

    //return View("UserProfile", BuildUserProfileViewModel(viewModel));
    return View("_VerifyOtp", await BuildVerifyOtpViewModel(viewModel)); 
  }

  [HttpPost]
  public async Task<ActionResult> VerifyOtp(ViewModels.VerifyOtp viewModel, string returnUrl)
  {
    User loginUser = await this.UserManager.Get(this.Context.Site, this.ControllerContext.HttpContext.User.GetUserId());

    if (loginUser == null)
    {
      return BadRequest();
    }

    MultifactorAuthenticationManager mfaManager = new();

    if (mfaManager.VerifyTOTP(loginUser.UserName, viewModel.OneTimePassword, loginUser.Secrets.EncryptedTotpSecretKey, loginUser.Secrets.TotpDigits, loginUser.Secrets.TotpPeriod, MultifactorAuthenticationManager.PREVIOUS_TIME_STEP_DELAY_DEFAULT, MultifactorAuthenticationManager.FUTURE_TIME_STEP_DELAY_DEFAULT))
    {
      return View("UserProfile", new ViewModels.UserProfile()
      {
        User = await this.UserManager.Get(this.Context.Site, this.ControllerContext.HttpContext.User.GetUserId()),
        ClaimTypeOptions = this.ClaimTypeOptions,
        ReturnUrl = returnUrl
      });
    }
    else
    {
      await Task.Delay(TimeSpan.FromSeconds(10));
      return Json(new { Title = "Login", Message = "Invalid one-time password.", Icon = "alert" });
    }
  }

  private ViewModels.UserProfile BuildUserProfileViewModel(ViewModels.UserProfile viewModel)
  {
    if (viewModel == null)
    {
      viewModel ??= new();
      //viewModel = new ViewModels.UserProfile();
    }

    return viewModel;
  }

  private async Task<ViewModels.VerifyOtp> BuildVerifyOtpViewModel(ViewModels.UserProfile inputViewModel) 
  {
    ViewModels.VerifyOtp viewModel = new();
    //ViewModels.VerifyOtp viewModel = new();
    User loginUser = await this.UserManager.Get(this.Context.Site, this.ControllerContext.HttpContext.User.GetUserId());
    UserSession session = await this.SessionManager.CreateNew(this.Context.Site, loginUser, false, ControllerContext.HttpContext.Connection.RemoteIpAddress);

    ////// When ready, move to another area/function/class etc where these properties are created (hidden from user) using constants?
    ////loginUser.Secrets.EncryptedTotpSecretKey = String.IsNullOrEmpty(loginUser.Secrets.EncryptedTotpSecretKey) ? OtpNet.Base32Encoding.ToString(System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())) : loginUser.Secrets.EncryptedTotpSecretKey;
    ////loginUser.Secrets.TotpSecretKeyEncryptionAlgorithm = String.IsNullOrEmpty(loginUser.Secrets.TotpSecretKeyEncryptionAlgorithm) ? "SHA1" : loginUser.Secrets.TotpSecretKeyEncryptionAlgorithm;
    ////loginUser.Secrets.TotpDigits = (loginUser.Secrets.TotpDigits != 6 || loginUser.Secrets.TotpDigits != 8) ? 6 : loginUser.Secrets.TotpDigits;
    ////loginUser.Secrets.TotpPeriod = (loginUser.Secrets.TotpPeriod != 30 || loginUser.Secrets.TotpDigits != 60) ? 30 : loginUser.Secrets.TotpPeriod;

    viewModel.Controller = "UserProfile";

    viewModel.ReturnUrl = inputViewModel.ReturnUrl;
    viewModel.Message = "";
    viewModel.SessionId = session.Id;


    MultifactorAuthenticationManager mfaManager = new();

    // TODO! When ready, move to another area/function/class etc where these properties are created (hidden from user) using constants?
    string encryptedTotpSecretKey = String.IsNullOrEmpty(loginUser.Secrets.EncryptedTotpSecretKey) ? OtpNet.Base32Encoding.ToString(System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())) : loginUser.Secrets.EncryptedTotpSecretKey;
    string totpSecretKeyEncryptionAlgorithm = String.IsNullOrEmpty(loginUser.Secrets.TotpSecretKeyEncryptionAlgorithm) ? "SHA1" : loginUser.Secrets.TotpSecretKeyEncryptionAlgorithm;
    int totpDigits = (loginUser.Secrets.TotpDigits != 6 || loginUser.Secrets.TotpDigits != 8) ? 6 : loginUser.Secrets.TotpDigits;
    int totpPeriod = (loginUser.Secrets.TotpPeriod != 30 || loginUser.Secrets.TotpDigits != 60) ? 30 : loginUser.Secrets.TotpPeriod;

    viewModel.QrCodeAsSvg = mfaManager.GenerateUserMFAQRCodeSetup(this.Context.Site.Name, loginUser.UserName, encryptedTotpSecretKey, totpSecretKeyEncryptionAlgorithm, totpDigits, totpPeriod);

    //viewModel.QrCodeAsSvg = mfaManager.GenerateUserMFAQRCodeSetup(this.Context.Site.Name, loginUser);

    return viewModel;
  }
}


