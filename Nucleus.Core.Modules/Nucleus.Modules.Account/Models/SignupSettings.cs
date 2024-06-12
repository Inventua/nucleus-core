using System;
using System.ComponentModel.DataAnnotations;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.Account.Models;

public class SignupSettings
{
  private class ModuleSettingsKeys
  {
    public const string MODULESETTING_RECAPTCHA_ENABLED = "signup:recaptcha-enabled";
    public const string MODULESETTING_RECAPTCHA_SITE_KEY = "signup:recaptcha-site-key";
    public const string MODULESETTING_RECAPTCHA_SECRET_KEY = "signup:recaptcha-secret-key";
    public const string MODULESETTING_RECAPTCHA_ACTION = "signup:recaptcha-action";
    public const string MODULESETTING_RECAPTCHA_SCORE_THRESHOLD = "signup:recaptcha-score-threshold";
  }
  
  public Boolean RecaptchaEnabled { get; set; }

  public string RecaptchaSiteKey { get; set; }

  private string RecaptchaEncryptedSecretKey { get; set; }

  public Boolean IsSecretKeySet
  {
    get
    {
      return !String.IsNullOrEmpty(this.RecaptchaEncryptedSecretKey);
    }
  }

  [RegularExpression("^[A-Za-z0-9/_]+$", ErrorMessage = "Action can only contain alphanumeric characters, slashes, and underscores.")]
  public string RecaptchaAction { get; set; }

  [Range(0.0, 1.0, ErrorMessage = "Score threshold must be between 0.0 and 1.0.")]
  public double RecaptchaScoreThreshold { get; set; }

  public string GetSecretKey(Site site)
  {
    if (String.IsNullOrEmpty(this.RecaptchaEncryptedSecretKey))
    {
      return "";
    }
    else
    {
      return DecryptSecretKey(site, this.RecaptchaEncryptedSecretKey);
    }
  }

  public void SetSecretKey(Site site, string key)
  {
    if (String.IsNullOrEmpty(key))
    {
      this.RecaptchaEncryptedSecretKey = "";
    }
    else
    {
      this.RecaptchaEncryptedSecretKey = EncryptSecretKey(site, key);
    }
  }

  public void ReadSettings(PageModule module)
  {
    this.RecaptchaEnabled = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_ENABLED, false);

    ReadRecaptchaSettings(module);
  }

  public void ReadEncryptedKeys(PageModule module)
  {
    this.RecaptchaEncryptedSecretKey = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_SECRET_KEY, "");
  }

  public void ReadRecaptchaSettings(PageModule module)
  {
    this.RecaptchaSiteKey = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_SITE_KEY, "");
    this.RecaptchaEncryptedSecretKey = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_SECRET_KEY, "");
    this.RecaptchaAction = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_ACTION, "");
    this.RecaptchaScoreThreshold = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_SCORE_THRESHOLD, 0.5);
  }

  public void SetSettings(Site site, PageModule module)
  {
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_ENABLED, this.RecaptchaEnabled);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_SITE_KEY, this.RecaptchaSiteKey);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_SECRET_KEY, this.RecaptchaEncryptedSecretKey);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_ACTION, this.RecaptchaAction);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_SCORE_THRESHOLD, this.RecaptchaScoreThreshold);
  }

  /// <summary>
  /// Encrypt and encode a secret key and return the result.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="secretKey"></param>
  /// <returns></returns>
  private static string EncryptSecretKey(Site site, string secretKey)
  {
    if (String.IsNullOrEmpty(secretKey))
    {
      return null;
    }

    // Convert string to byte array
    byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes(secretKey);

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
  /// Decrypt and decode a secret key and return the result.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="secretKey"></param>
  /// <returns></returns>
  private static string DecryptSecretKey(Site site, string secretKey)
  {
    if (String.IsNullOrEmpty(secretKey))
    {
      return null;
    }

    // Convert string to byte array
    byte[] bytesIn = System.Convert.FromBase64String(secretKey);

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
