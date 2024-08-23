using System;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Mail;
using Azure.Communication.Email;

namespace Nucleus.Extensions.AzureEmailCommunications;

/// <summary>
/// Settings for the Twilio AzureEmailCommunications service.
/// </summary>
public class AzureEmailCommunicationsOptions : IMailSettings
{
  /// <summary>
  /// Configuration file section path for AzureEmailCommunications options.
  /// </summary>
  public const string Section = "Nucleus:Mail:AzureEmailCommunicationsMailOptions";

  public const string UNCHANGED_CONNECTIONSTRING = "#$UNCHANGED";

  private static class AzureEmailCommunicationsMailSettingKeys
  {
    public const string AZUREEMAILCOMMUNICATIONS_SENDER = "mail:azureemailcommunications:sender";
    public const string AZUREEMAILCOMMUNICATIONS_CONNECTIONSTRING = "mail:azureemailcommunications:connection-string";
  }


  private string _connectionString;
  
  /// <summary>
  /// Azure connection string
  /// </summary>
  public string ConnectionString
  {
    get
    {
      return _connectionString;
    }
    set
    {
      if (value != UNCHANGED_CONNECTIONSTRING)
      {
        _connectionString = value;
      }
    }
  }

  /// <summary>
  /// Mail sender name (email address)
  /// </summary>
  public string Sender { get; set; }

  public void GetSettings(Site site)
  {
    if (site.SiteSettings.TryGetValue(AzureEmailCommunicationsMailSettingKeys.AZUREEMAILCOMMUNICATIONS_SENDER, out string sender))
    {
      this.Sender = sender;
    }

    if (site.SiteSettings.TryGetValue(AzureEmailCommunicationsMailSettingKeys.AZUREEMAILCOMMUNICATIONS_CONNECTIONSTRING, out string connectionString))
    {
      this.ConnectionString = Decrypt(site, connectionString);
    }
  }

  public void SetSettings(Site site)
  {
    site.SiteSettings.TrySetValue(AzureEmailCommunicationsMailSettingKeys.AZUREEMAILCOMMUNICATIONS_SENDER, this.Sender);

    if (this.ConnectionString != UNCHANGED_CONNECTIONSTRING)
    {
      site.SiteSettings.TrySetValue(AzureEmailCommunicationsMailSettingKeys.AZUREEMAILCOMMUNICATIONS_CONNECTIONSTRING, Encrypt(site, this.ConnectionString));
    }
  }

  /// <summary>
  /// Encrypt and encode a value and return the result.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="value"></param>
  /// <returns></returns>
  private static string Encrypt(Site site, string value)
  {
    if (String.IsNullOrEmpty(value))
    {
      return null;
    }

    // Convert string to byte array
    byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes(value);

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
  /// Encrypt and encode a value and return the result.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="encryptedValue"></param>
  /// <returns></returns>
  private static string Decrypt(Site site, string encryptedValue)
  {
    if (String.IsNullOrEmpty(encryptedValue))
    {
      return null;
    }

    // Convert string to byte array
    byte[] bytesIn = System.Convert.FromBase64String(encryptedValue);

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
