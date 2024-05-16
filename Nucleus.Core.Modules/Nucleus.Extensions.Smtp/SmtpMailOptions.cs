using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Mail;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions.Smtp;

/// <summary>
/// Represents settings used to communicate with a (mail) server.
/// </summary>
public class SmtpMailOptions : IMailSettings // <SmtpMailOptions>
{
  public const string Section = "Nucleus:Mail:SmtpMailOptions";

  /// <summary>
  /// Value used to represent a password whose value has not been changed.
  /// </summary>
  public const string UNCHANGED_PASSWORD = "#$UNCHANGED";

  public const int DEFAULT_SMTP_PORT = 587;

  private static class SmtpClientSettingsKeys
  {
    /// <summary>
    /// SMTP server host name
    /// </summary>
    public const string MAIL_HOSTNAME = "mail:hostname";
    /// <summary>
    /// SMTP server port name
    /// </summary>
    public const string MAIL_PORT = "mail:port";
    /// <summary>
    /// Use SSL to connect 
    /// </summary>
    public const string MAIL_USESSL = "mail:usessl";
    /// <summary>
    /// Default mail sender name
    /// </summary>
    public const string MAIL_SENDER = "mail:sender";
    /// <summary>
    /// SMTP server user name
    /// </summary>
    public const string MAIL_USERNAME = "mail:username";
    /// <summary>
    /// SMTP server password
    /// </summary>
    public const string MAIL_PASSWORD = "mail:password";

    /// <summary>
    /// Default SMTP server port.
    /// </summary>
    public const int DEFAULT_SMTP_PORT = 587;
  }

  /// <summary>
  /// SMTP (mail) server host name
  /// </summary>
  public string HostName { get; set; }

  /// <summary>
  /// SMTP (mail) server port
  /// </summary>
  public int Port { get; set; }

  /// <summary>
  /// Specifies whether to use SSL for connection to the mail server.
  /// </summary>
  public Boolean UseSsl { get; set; }


  /// <summary>
  /// Mail sender name (email address)
  /// </summary>
  public string Sender { get; set; }

  /// <summary>
  /// SMTP server user name
  /// </summary>
  public string UserName { get; set; }

  private string _password;
  /// <summary>
  /// SMTP server password
  /// </summary>
  public string Password 
  { 
    get 
    {
      return _password;
    } 
    set 
    {
      if (value != UNCHANGED_PASSWORD)
      {
        _password = value;
      }
    } 
  }

  /// <summary>
  /// Read settings values from site settings
  /// </summary>
  /// <param name="site"></param>
  public void GetSettings(Site site)
  {
    
    if (site.SiteSettings.TryGetValue(SmtpClientSettingsKeys.MAIL_HOSTNAME, out string hostName))
    {
      this.HostName = hostName;
    }

    if (site.SiteSettings.TryGetValue(SmtpClientSettingsKeys.MAIL_PORT, out int port))
    {
      this.Port = port;
    }
    if (this.Port == 0)
    {
      this.Port = SmtpMailOptions.DEFAULT_SMTP_PORT;
    }

    if (site.SiteSettings.TryGetValue(SmtpClientSettingsKeys.MAIL_USESSL, out bool useSsl))
    {
      this.UseSsl = useSsl;
    }

    if (site.SiteSettings.TryGetValue(SmtpClientSettingsKeys.MAIL_SENDER, out string sender))
    {
      this.Sender = sender;
    }

    if (site.SiteSettings.TryGetValue(SmtpClientSettingsKeys.MAIL_USERNAME, out string username))
    {
      this.UserName = username;
    }

    if (site.SiteSettings.TryGetValue(SmtpClientSettingsKeys.MAIL_PASSWORD, out string password))
    {
      this.Password = DecryptPassword(site, password);
    }
  }


  /// <summary>
  /// Set site settings values from this instance
  /// </summary>
  /// <param name="site"></param>
  public void SetSettings(Site site)
  {
    site.SiteSettings.TrySetValue(SmtpClientSettingsKeys.MAIL_HOSTNAME, this.HostName);
    site.SiteSettings.TrySetValue(SmtpClientSettingsKeys.MAIL_PORT, this.Port);
    site.SiteSettings.TrySetValue(SmtpClientSettingsKeys.MAIL_USESSL, this.UseSsl);

    site.SiteSettings.TrySetValue(SmtpClientSettingsKeys.MAIL_SENDER, this.Sender);
    site.SiteSettings.TrySetValue(SmtpClientSettingsKeys.MAIL_USERNAME, this.UserName);

    if (this.Password != UNCHANGED_PASSWORD)
    {
      site.SiteSettings.TrySetValue(Site.SiteMailSettingKeys.MAIL_PASSWORD, EncryptPassword(site, this.Password));
    }
  }


  /// <summary>
  /// Encrypt and encode a password and return the result.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="password"></param>
  /// <returns></returns>
  private static string EncryptPassword(Site site, string password)
  {
    if (String.IsNullOrEmpty(password))
    {
      return null;
    }

    // Convert string to byte array
    byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes(password);

    // Preparing the memory stream for encrypted string.
    System.IO.MemoryStream msOut = new();

    // Create the ICryptoTransform instance.
    System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create();
    aes.Key = site.Id.ToByteArray();
    aes.IV = site.Id.ToByteArray();

    // Create the CryptoStream instance.
    System.Security.Cryptography.CryptoStream cryptStream = new(msOut, aes.CreateEncryptor(aes.Key, aes.IV), System.Security.Cryptography.CryptoStreamMode.Write);

    // Encoding.
    cryptStream.Write(bytesIn, 0, bytesIn.Length);
    cryptStream.FlushFinalBlock();

    // Get the encrypted byte array.
    byte[] bytesOut = msOut.ToArray();

    cryptStream.Close();
    msOut.Close();

    // Convert to string and return result value
    return System.Convert.ToBase64String(bytesOut);
  }

  /// <summary>
  /// Encrypt and encode a password and return the result.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="password"></param>
  /// <returns></returns>
  private static string DecryptPassword(Site site, string password)
  {
    if (String.IsNullOrEmpty(password))
    {
      return null;
    }

    // Convert string to byte array
    byte[] bytesIn = System.Convert.FromBase64String(password);

    // Preparing the memory stream for encrypted string.
    System.IO.MemoryStream msOut = new();

    // Create the ICryptoTransform instance.
    System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create();
    aes.Key = site.Id.ToByteArray();
    aes.IV = site.Id.ToByteArray();

    // Create the CryptoStream instance.
    System.Security.Cryptography.CryptoStream cryptStream = new(msOut, aes.CreateDecryptor(aes.Key, aes.IV), System.Security.Cryptography.CryptoStreamMode.Write);

    // Encoding.
    cryptStream.Write(bytesIn, 0, bytesIn.Length);
    cryptStream.FlushFinalBlock();

    // Get the encrypted byte array.
    byte[] bytesOut = msOut.ToArray();

    cryptStream.Close();
    msOut.Close();

    // Convert to string and return result value
    return System.Text.Encoding.UTF8.GetString(bytesOut);
  }
}
