using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Nucleus.Modules.Account;

public class GoogleRecaptchaHandler
{
  private const string GOOGLE_RECAPTCHA_VERIFY_SITE = "https://www.google.com/recaptcha/api/siteverify";

  private Boolean Enabled { get; set; }
  private string SecretKey { get; set; }
  private string Action { get; set; }
  private double ScoreThreshold { get; set; }
  private IHttpClientFactory HttpClientFactory { get; }
  private string RemoteIpAddress { get; set; }
  private ILogger Logger { get; }

  public GoogleRecaptchaHandler(IHttpClientFactory httpClientFactory, ILogger logger, string secretKey, string action, double scoreThreshold, string remoteIpAddress)
  {
    //this.SiteKey = siteKey;
    this.HttpClientFactory = httpClientFactory;
    this.SecretKey = secretKey;
    this.Action = action;
    this.ScoreThreshold = scoreThreshold;
    this.RemoteIpAddress = remoteIpAddress;
  }

  public async Task<Models.SiteVerifyResponseToken> VerifyToken(string verificationToken)
  {
    Models.SiteVerifyResponseToken responseToken;

    try
    {
      responseToken = await VerifyRecaptchaToken(verificationToken);

      // Log errors if verification failed
      if (!responseToken.Success)
      {
        if (responseToken.ErrorCodes.Count > 0)
        {
          this.Logger?.LogWarning("Unsuccessful reCAPTCHA verification.  Errors: '{errorCodes}'.", String.Join(',', responseToken.ErrorCodes));
        }

        if (responseToken.Action != this.Action)
        {
          this.Logger?.LogWarning("Unexpected reCAPTCHA action in response.  Expected: '{expectedAction}', received: '{receivedAction}'.", this.Action, responseToken.Action);
        }

        if (responseToken.Score < Convert.ToSingle(this.ScoreThreshold))
        {
          this.Logger?.LogWarning("Suspected robot from {address}. Recaptcha verify score was {score}. Action: {action}.", this.RemoteIpAddress, responseToken.Score, responseToken.Action);
        }
      }

      return responseToken;
    }
    catch (Exception ex)
    {
      this.Logger?.LogError(ex, "Verifying reCAPTCHA token.");
      return new () { Success = false, Score = 0, Action = this.Action };
    }
  }

  /// <summary>
  /// Verify the user response token.
  /// </summary>
  /// <param name="recaptchaToken"></param>
  /// <returns></returns>
  private async Task<Models.SiteVerifyResponseToken> VerifyRecaptchaToken(string recaptchaToken) 
  {
    HttpResponseMessage responseMessage;

    using (HttpClient httpClient = this.HttpClientFactory.CreateClient())
    {
      responseMessage = await httpClient.PostAsync($"{GOOGLE_RECAPTCHA_VERIFY_SITE}",
        new FormUrlEncodedContent(new[]
        {
          new KeyValuePair<string, string>("secret", this.SecretKey),
          new KeyValuePair<string, string>("response", recaptchaToken),
          new KeyValuePair<string, string>("remoteip", this.RemoteIpAddress)
        })
      );
    }

    responseMessage.EnsureSuccessStatusCode();

    Models.SiteVerifyResponseToken responseToken = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.SiteVerifyResponseToken>(await responseMessage.Content.ReadAsStringAsync());

    return responseToken;
  }
}
