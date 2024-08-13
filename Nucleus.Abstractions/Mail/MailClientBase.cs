using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;

namespace Nucleus.Abstractions.Mail;

/// <summary>
/// Base implementation of <see cref="IMailClient"/> which includes an implementation of the <see cref="Site"/> property, and shared methods used to create emails.
/// </summary>
public abstract class MailClientBase : IMailClient
{
  /// <summary>
  /// Specifies the <seealso cref="Site"/> that is using this instance.
  /// </summary>
  public Site Site { get; set; }

  /// <summary>
  /// Generate a fully qualified path to the settings (cshtml) partial view for the mail client.
  /// </summary>
  /// <remarks>
  /// Return null or an empty string if there is no settings view for the mail client.
  /// </remarks>
  /// <returns></returns>
  public abstract string SettingsPath { get; }

  /// <summary>
  /// Return the extended settings type for the mail client.
  /// </summary>
  /// <remarks>
  /// Returns null if there is no settings view for the mail client.
  /// </remarks>
  public abstract IMailSettings GetSettings(Site site);

  /// <summary>
  /// Save the settings for the mail client.
  /// </summary>
  public abstract void SetSettings(Site site, IMailSettings settings);

  /// <summary>
  /// Returns a style element (as a string) containing default mail css.
  /// </summary>
  /// <returns></returns>
  /// <remarks>
  /// The returned style element is added to the lt;head&gt; element of generated emails.
  /// </remarks>
  public string BuildDefaultCss()
  {
    Type type = typeof(MailClientBase);

    using (System.IO.Stream cssStream = type.Assembly.GetManifestResourceStream(type.FullName.Replace(type.Name, "mail.css")))
    {
      System.IO.StreamReader reader = new(cssStream);
      return "<style>" + reader.ReadToEnd() + "</style>";
    }
  }
  
  /// <summary>
  /// Dispose of resources.
  /// </summary>
  public virtual void Dispose() { }

  /// <summary>
  /// Parse the specified template, and send the resulting email to the specified to address.
  /// </summary>
  /// <param name="template"></param>
  /// <param name="model"></param>
  /// <param name="to"></param>
  /// <typeparam name="TModel"></typeparam>
  public abstract Task Send<TModel>(MailTemplate template, TModel model, string to)  where TModel : class;

}
