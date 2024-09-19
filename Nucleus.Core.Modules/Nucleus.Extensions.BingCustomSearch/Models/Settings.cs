using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions.BingCustomSearch.Models;

public class Settings
{
  private class SiteSettingsKeys
  {
    public const string SITESETTING_API_KEY = "bingcustomsearch:apikey";
    public const string SITESETTING_CONFIGURATION_ID = "bingcustomsearch:configurationid";
    public const string SITESETTING_SAFE_SEARCH = "bingcustomsearch:safesearch";
  }

  public const string SEARCH_BASE_URI = "https://api.bing.microsoft.com/v7.0/custom/search";
  public const string SUGGEST_BASE_URI = "https://api.bing.microsoft.com/v7.0/custom/suggestions/search";

  public enum SafeSearchOptions
  {
    Off = 1,
    Moderate = 2,
    Strict = 3
  }

  public string EncryptedApiKey { get; set; }
  public string ConfigurationId { get; set; }

  public SafeSearchOptions SafeSearch { get; set; }

  public Boolean IsApiKeySet
  {
    get
    {
      return !String.IsNullOrEmpty(this.EncryptedApiKey);
    }
  }

  public void SetApiKey(Site site, string apiKey)
  {
    if (string.IsNullOrEmpty(apiKey))
    {
      this.EncryptedApiKey = "";
    }
    else
    {
      this.EncryptedApiKey = EncryptApiKey(site, apiKey);
    }
  }

  public string GetApiKey(Site site)
  {
    if (String.IsNullOrEmpty(this.EncryptedApiKey))
    {
      return "";
    }
    else
    {
      return DecryptApiKey(site, this.EncryptedApiKey);
    }
  }


  public void GetSettings(Site site)
  {
    if (site.SiteSettings.TryGetValue(SiteSettingsKeys.SITESETTING_API_KEY, out string apiKey))
    {
      this.EncryptedApiKey = apiKey;
    }

    if (site.SiteSettings.TryGetValue(SiteSettingsKeys.SITESETTING_CONFIGURATION_ID, out string configurationId))
    {
      this.ConfigurationId = configurationId;
    }

    if (site.SiteSettings.TryGetValue(SiteSettingsKeys.SITESETTING_SAFE_SEARCH, out string safeSearch))
    {
      if (Enum.TryParse<SafeSearchOptions>(safeSearch, out SafeSearchOptions safeSearchEnum))
      {
        this.SafeSearch = safeSearchEnum;
      }
    }
  }

  public void ReadEncryptedApiKey(Site site)
  {
    if (site.SiteSettings.TryGetValue(SiteSettingsKeys.SITESETTING_API_KEY, out string apiKey))
    {
      this.EncryptedApiKey = apiKey;
    }
  }


  public void SetSettings(Site site)
  {
    site.SiteSettings.TrySetValue(SiteSettingsKeys.SITESETTING_API_KEY, this.EncryptedApiKey);
    site.SiteSettings.TrySetValue(SiteSettingsKeys.SITESETTING_CONFIGURATION_ID, this.ConfigurationId);
    site.SiteSettings.TrySetValue(SiteSettingsKeys.SITESETTING_SAFE_SEARCH, this.SafeSearch.ToString());
  }

  /// <summary>
  /// Encrypt and encode a secret key and return the result.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="apiKey"></param>
  /// <returns></returns>
  private static string EncryptApiKey(Site site, string apiKey)
  {
    if (String.IsNullOrEmpty(apiKey))
    {
      return null;
    }

    // Convert string to byte array
    byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes(apiKey);

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
  private static string DecryptApiKey(Site site, string apiKey)
  {
    if (String.IsNullOrEmpty(apiKey))
    {
      return null;
    }

    // Convert string to byte array
    byte[] bytesIn = System.Convert.FromBase64String(apiKey);

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
