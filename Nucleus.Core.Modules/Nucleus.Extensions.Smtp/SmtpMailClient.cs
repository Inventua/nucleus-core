using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Extensions;
using Nucleus.Abstractions.Models.Configuration;
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
[System.ComponentModel.DisplayName("SMTP")]
public class SmtpMailClient : MailClientBase
{
  private IOptions<SmtpMailOptions> Options { get; }

  private ILogger<SmtpMailClient> Logger { get; }

  public SmtpMailClient(IOptions<SmtpMailOptions> smtpMailOptions, ILogger<SmtpMailClient> logger)
  {
    this.Options = smtpMailOptions;
    this.Logger = logger;
  }
    
  public override string SettingsPath => "/Extensions/Smtp Mail/Views/_SmtpSettings.cshtml";

  public override IMailSettings GetSettings(Site site)
  {
    this.Options.Value.GetSettings(site);
    return this.Options.Value;
  }

  public override void SetSettings(Site site, IMailSettings settings)
  {
    (settings as SmtpMailOptions)?.SetSettings(site);
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

    // Populate the options object from site settings
    this.Options.Value.GetSettings(this.Site);

    if (String.IsNullOrEmpty(this.Options.Value.Sender))
    {
      throw new InvalidOperationException("Mail not sent because a sender address is not configured for the site.");
    }

    message.From.Add(MimeKit.MailboxAddress.Parse(this.Options.Value.Sender));

    foreach (string address in to.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
    {
      message.To.Add(MimeKit.MailboxAddress.Parse(address));
    }

    message.Subject = await template.Subject.ParseTemplate<TModel>(model);
    builder.HtmlBody = BuildDefaultCss() + await template.Body.ParseTemplate<TModel>(model);

    message.Body = builder.ToMessageBody();
        
    using (MailKit.Net.Smtp.SmtpClient client = new(new ProtocolLogger(this.Logger)))
    {
      client.Connect(this.Options.Value.HostName, this.Options.Value.Port, MailKit.Security.SecureSocketOptions.Auto);
      client.Authenticate(this.Options.Value.UserName, this.Options.Value.Password);

      client.Send(message);

      client.Disconnect(true);
    }
  }

  internal class ProtocolLogger : IProtocolLogger
  {
    public IAuthenticationSecretDetector AuthenticationSecretDetector { get; set; }
    private ILogger<SmtpMailClient> Logger { get; }

    internal ProtocolLogger(ILogger<SmtpMailClient> logger)
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
