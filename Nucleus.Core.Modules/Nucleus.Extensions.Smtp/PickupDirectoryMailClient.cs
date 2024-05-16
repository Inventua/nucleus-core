using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Microsoft.Extensions.Options;
using MailKit;
using Nucleus.Abstractions.Mail;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Extensions.Razor;

namespace Nucleus.Extensions.Smtp;

/// <summary>
/// Allows Nucleus core and extensions to send email using SMTP.
/// </summary>
[System.ComponentModel.DisplayName("Pickup Directory")]
public class PickupDirectoryMailClient : MailClientBase
{
  private IOptions<PickupDirectoryMailOptions> Options { get; }

  private ILogger<PickupDirectoryMailClient> Logger { get; }
    
  public PickupDirectoryMailClient(IOptions<PickupDirectoryMailOptions> options, ILogger<PickupDirectoryMailClient> logger)
  {
    this.Options = options;
    this.Logger = logger;
  }

  public override string SettingsPath => "/Extensions/Smtp Mail/Views/_PickupDirectorySettings.cshtml";

  public override IMailSettings GetSettings(Site site)
  {  
    this.Options.Value.GetSettings(site); 
    return this.Options.Value;    
  }

  public override void SetSettings(Site site, IMailSettings settings)
  {
    (settings as PickupDirectoryMailOptions)?.SetSettings(site);
  }

  /// <summary>
  /// Parse the specified template, and send the resulting email to the specified 'to' address. The 'to' address can be a list 
  /// of email addresses separated by commas or semicolons.
  /// </summary>
  /// <param name="template"></param>
  /// <param name="args"></param>
  /// <param name="to"></param>
  public override async Task Send<TModel>(MailTemplate template, TModel model, string to)
    where TModel : class
  {
    MimeKit.MimeMessage message = new();
    MimeKit.BodyBuilder builder = new();

    if (this.Site == null)
    {
      throw new InvalidOperationException($"{nameof(this.Site)} cannot be null");
    }

    // Get site settings
    this.Options.Value.GetSettings(this.Site);
    //mailSettings.Password = this.Site.GetSmtpMailPassword();

    if (String.IsNullOrEmpty(this.Options.Value.Sender))
    {
      throw new InvalidOperationException("Mail not sent because a sender address is not configured.");
    }

    message.From.Add(MimeKit.MailboxAddress.Parse(this.Options.Value.Sender));

    foreach (string address in to.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
    {
      message.To.Add(MimeKit.MailboxAddress.Parse(address));
    }
    
    message.Subject = await template.Subject.ParseTemplate<TModel>(model);
    builder.HtmlBody = BuildDefaultCss() + await template.Body.ParseTemplate<TModel>(model);

    message.Body = builder.ToMessageBody();       
    message.WriteTo(System.IO.Path.Combine(this.Options.Value.PickupDirectoryLocation, $"{Guid.NewGuid()}.eml"));   
  }

  internal class ProtocolLogger : IProtocolLogger
  {
    public IAuthenticationSecretDetector AuthenticationSecretDetector { get; set; }
    private ILogger<PickupDirectoryMailClient> Logger { get; }

    internal ProtocolLogger(ILogger<PickupDirectoryMailClient> logger)
    {
      this.Logger = logger;
    }

    public void Dispose()
    {

    }

    private string Decode(byte[] buffer, int offset, int count)
    {
      string result = System.Text.Encoding.UTF8.GetString(buffer, offset, count);

      // remove trailing CRLF, as our logging components add a CRLF automatically
      if (result.EndsWith("\r\n"))
      {
        result = result[0..^2];
      }
      return result;
    }

    public void LogClient(byte[] buffer, int offset, int count)
    {
      this?.Logger.LogTrace(Decode(buffer, offset, count));
    }

    public void LogConnect(Uri uri)
    {
      this?.Logger.LogTrace("Connected to {uri}.", uri);
    }

    public void LogServer(byte[] buffer, int offset, int count)
    {
      this?.Logger.LogTrace(Decode(buffer, offset, count));
    }
  }
}
