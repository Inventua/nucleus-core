using System;
using System.Threading.Tasks;
using Nucleus.Abstractions.Mail;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using SendGrid;
using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;
using Nucleus.Extensions.Razor;

namespace Nucleus.Extensions.SendGrid;

/// <summary>
/// Mail client for Twilio SendGrid service.
/// </summary>
[System.ComponentModel.DisplayName("Twilio SendGrid")]
public class SendGridMailClient : MailClientBase
{
  private IOptions<SendGridMailOptions> Options { get; }
    
  public SendGridMailClient (IOptions<SendGridMailOptions> options)
  {
    this.Options = options;
  }

  public override string SettingsPath => "/Extensions/SendGrid Mail/Views/_SendGridSettings.cshtml";

  public override IMailSettings GetSettings(Site site)
  {
    this.Options.Value.GetSettings(site);
    return this.Options.Value;    
  }

  public override void SetSettings(Site site, IMailSettings settings)
  {
    (settings as SendGridMailOptions).SetSettings(site);
  }

  public override async Task Send<TModel>(MailTemplate template, TModel model, string to) where TModel : class
  {
    if (this.Site == null)
    {
      throw new InvalidOperationException($"{nameof(this.Site)} cannot be null");
    }

    // Get site settings
    this.Options.Value.GetSettings(this.Site);

    if (String.IsNullOrEmpty(this.Options.Value.Sender))
    {
      throw new InvalidOperationException("Mail not sent because a sender address is not configured for the site.");
    }

    SendGridClient client = new(new SendGridClientOptions()
    {
      ApiKey = this.Options.Value.ApiKey,
      Host = this.Options.Value.Host,
      HttpErrorAsException = true
    });

    SendGridMessage message = new() 
    { 
       From = new(this.Options.Value.Sender)
    };

    foreach (string address in to.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
    {
      message.AddTo(address);
    }

    message.Subject = await template.Subject.ParseTemplate<TModel>(model);
    message.AddContent("text/html", BuildDefaultCss() + await template.Body.ParseTemplate<TModel>(model));

    await client.SendEmailAsync(message);
  }
}
