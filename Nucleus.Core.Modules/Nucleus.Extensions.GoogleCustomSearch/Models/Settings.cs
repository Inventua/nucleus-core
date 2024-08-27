using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.CustomSearchAPI.v1;

namespace Nucleus.Extensions.GoogleCustomSearch.Models;

public class Settings
{
  private class SiteSettingsKeys
  {
    public const string SITESETTING_API_KEY = "googlecustomsearch:apikey";
    public const string SITESETTING_SEARCH_ENGINE_ID = "googlecustomsearch:searchengineid";
    public const string SITESETTING_SAFE_SEARCH = "googlecustomsearch:safesearch";
  }

  private string EncryptedApiKey { get; set; }

  public string SearchEngineId { get; set; }

  public CseResource.ListRequest.SafeEnum SafeSearch { get; set; } = CseResource.ListRequest.SafeEnum.Active;


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

    if (site.SiteSettings.TryGetValue(SiteSettingsKeys.SITESETTING_SEARCH_ENGINE_ID, out string searchEngineId))
    {
      this.SearchEngineId = searchEngineId;
    }

    if (site.SiteSettings.TryGetValue(SiteSettingsKeys.SITESETTING_SAFE_SEARCH, out string safeSearch))
    {
      if (Enum.TryParse<CseResource.ListRequest.SafeEnum>(safeSearch, out CseResource.ListRequest.SafeEnum safeSearchEnum))
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
    site.SiteSettings.TrySetValue(SiteSettingsKeys.SITESETTING_SEARCH_ENGINE_ID, this.SearchEngineId);
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
