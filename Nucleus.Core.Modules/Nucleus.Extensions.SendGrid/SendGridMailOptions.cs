using System;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Mail;

namespace Nucleus.Extensions.SendGrid;

/// <summary>
/// Settings for the Twilio SendGrid service.
/// </summary>
public class SendGridMailOptions : IMailSettings
{
  /// <summary>
  /// Configuration file section path for SendGrid options.
  /// </summary>
  public const string Section = "Nucleus:Mail:SendGridMailOptions";

  public const string UNCHANGED_APIKEY = "#$UNCHANGED";

  private static class SendGridMailSettingKeys
  {
    public const string SENDGRID_HOSTNAME = "mail:sendgrid:hostname";
    public const string SENDGRID_SENDER = "mail:sendgrid:sender";
    public const string SENDGRID_APIKEY = "mail:sendgrid:apikey";
  }

  public string Host { get; set; } = "https://api.sendgrid.com/";


  private string _apiKey;
  /// <summary>
  /// SMTP server password
  /// </summary>
  public string ApiKey
  {
    get
    {
      return _apiKey;
    }
    set
    {
      if (value != UNCHANGED_APIKEY)
      {
        _apiKey = value;
      }
    }
  }

  /// <summary>
  /// Mail sender name (email address)
  /// </summary>
  public string Sender { get; set; }

  public void GetSettings(Site site)
  {
    if (site.SiteSettings.TryGetValue(SendGridMailSettingKeys.SENDGRID_HOSTNAME, out string host))
    {
      this.Host = host;
    }

    if (site.SiteSettings.TryGetValue(SendGridMailSettingKeys.SENDGRID_SENDER, out string sender))
    {
      this.Sender = sender;
    }

    if (site.SiteSettings.TryGetValue(SendGridMailSettingKeys.SENDGRID_APIKEY, out string apiKey))
    {
      this.ApiKey = DecryptPassword(site, apiKey);
    }
  }

  public void SetSettings(Site site)
  {
    site.SiteSettings.TrySetValue(SendGridMailSettingKeys.SENDGRID_HOSTNAME, this.Host);
    site.SiteSettings.TrySetValue(SendGridMailSettingKeys.SENDGRID_SENDER, this.Sender);

    if (this.ApiKey != UNCHANGED_APIKEY)
    {
      site.SiteSettings.TrySetValue(SendGridMailSettingKeys.SENDGRID_APIKEY, EncryptPassword(site, this.ApiKey));
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
