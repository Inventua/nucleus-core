using System;
using System.Threading.Tasks;
using Nucleus.Abstractions.Mail;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Microsoft.Extensions.Options;
using Nucleus.Extensions.Razor;
using Azure.Communication.Email;

namespace Nucleus.Extensions.AzureEmailCommunications;

/// <summary>
/// Mail client for Twilio AzureEmailCommunications service.
/// </summary>
[System.ComponentModel.DisplayName("Azure Email Communications")]
public class AzureEmailCommunicationsClient : MailClientBase
{
  private IOptions<AzureEmailCommunicationsOptions> Options { get; }
    
  public AzureEmailCommunicationsClient (IOptions<AzureEmailCommunicationsOptions> options)
  {
    this.Options = options;
  }

  public override string SettingsPath => "/Extensions/AzureEmailCommunications/Views/_AzureEmailCommunicationsSettings.cshtml";

  public override IMailSettings GetSettings(Site site)
  {
    this.Options.Value.GetSettings(site);
    return this.Options.Value;    
  }

  public override void SetSettings(Site site, IMailSettings settings)
  {
    (settings as AzureEmailCommunicationsOptions).SetSettings(site);
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

    Azure.Communication.Email.EmailClient client = new(this.Options.Value.ConnectionString);

    EmailRecipients recipients = new();
    foreach (string address in to.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
    {
      recipients.To.Add(new EmailAddress(address));
    }

    EmailContent emailContent = new(await template.Subject.ParseTemplate<TModel>(model));
    emailContent.Html = BuildDefaultCss() + await template.Body.ParseTemplate<TModel>(model);

    EmailMessage message = new(this.Options.Value.Sender, recipients, emailContent);

    await client.SendAsync(Azure.WaitUntil.Completed, message);
  }
}
