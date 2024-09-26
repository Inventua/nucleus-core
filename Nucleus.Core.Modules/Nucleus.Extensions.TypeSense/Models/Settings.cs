using System;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions.TypeSense.Models;
public class Settings
{
  internal const string SITESETTING_SERVER_URL = "typesense:server-url";
  internal const string SITESETTING_INDEX_NAME = "typesense:indexname";
  internal const string SITESETTING_SERVER_APIKEY = "typesense:apikey";
  internal const string SITESETTING_INDEXER_NAME = "typesense:indexername";

  internal const string SITESETTING_ATTACHMENT_MAXSIZE = "typesense:attachment-maxsize";
  internal const string SITESETTING_INDEXING_PAUSE = "typesense:indexing-pause";

  internal const string SITESETTING_TIKA_SERVER_URL = "typesense:tika-server-url";

  internal const string SITESETTING_BOOST_TITLE = "typesense:boost-title";
  internal const string SITESETTING_BOOST_SUMMARY = "typesense:boost-summary";
  internal const string SITESETTING_BOOST_CATEGORIES = "typesense:boost-categories";
  internal const string SITESETTING_BOOST_KEYWORDS = "typesense:boost-keywords";
  internal const string SITESETTING_BOOST_CONTENT = "typesense:boost-content";

  public string ServerUrl { get; set; }

  public string IndexName { get; set; }

  public string IndexerName { get; set; }

  public int AttachmentMaxSize { get; set; } = 32;

  public string EncryptedApiKey { get; set; }

  public double IndexingPause { get; set; } = 1;

  public string TikaServerUrl { get; set; }

  public Nucleus.Abstractions.Search.SearchQuery.BoostSettings Boost { get; set; } = new();

  // This constructor is used by model binding
  public Settings() { }

  public Settings(Site site)
  {
    if (site.SiteSettings.TryGetValue(SITESETTING_SERVER_URL, out string serverUrl))
    {
      this.ServerUrl = serverUrl;
    }

    if (site.SiteSettings.TryGetValue(SITESETTING_INDEX_NAME, out string indexName))
    {
      this.IndexName = indexName;
    }

    if (site.SiteSettings.TryGetValue(SITESETTING_INDEXER_NAME, out string indexerName))
    {
      this.IndexerName = indexerName;
    }

    if (site.SiteSettings.TryGetValue(SITESETTING_SERVER_APIKEY, out string encryptedApiKey))
    {
      this.EncryptedApiKey = encryptedApiKey;
    }

    if (site.SiteSettings.TryGetValue(SITESETTING_ATTACHMENT_MAXSIZE, out string maxSize))
    {
      if (int.TryParse(maxSize, out int attachmentMaxSize))
      {
        this.AttachmentMaxSize = attachmentMaxSize;
      }
    }

    if (site.SiteSettings.TryGetValue(SITESETTING_INDEXING_PAUSE, out string indexingPause))
    {
      if (double.TryParse(indexingPause, out double indexingPauseSeconds))
      {
        this.IndexingPause = indexingPauseSeconds;
      }
    }

    if (site.SiteSettings.TryGetValue(SITESETTING_TIKA_SERVER_URL, out string tikaServerUrl))
    {
      this.TikaServerUrl = tikaServerUrl;
    }


    this.Boost.Title = GetSetting(site, SITESETTING_BOOST_TITLE, this.Boost.Title);
    this.Boost.Summary = GetSetting(site, SITESETTING_BOOST_SUMMARY, this.Boost.Summary);
    this.Boost.Categories = GetSetting(site, SITESETTING_BOOST_CATEGORIES, this.Boost.Categories);
    this.Boost.Keywords = GetSetting(site, SITESETTING_BOOST_KEYWORDS, this.Boost.Keywords);
    this.Boost.Content = GetSetting(site, SITESETTING_BOOST_CONTENT, this.Boost.Content);

  }

  public void SaveSettings(Site site, string apiKey)
  {
    if (!this.ServerUrl.StartsWith("http"))
    {
      this.ServerUrl = "http://" + this.ServerUrl;
    }

    site.SiteSettings.TrySetValue(SITESETTING_SERVER_URL, this.ServerUrl);
    site.SiteSettings.TrySetValue(SITESETTING_INDEX_NAME, this.IndexName);

    if (apiKey != ViewModels.Settings.DUMMY_APIKEY)
    {
      site.SiteSettings.TrySetValue(SITESETTING_SERVER_APIKEY, EncryptApiKey(site, apiKey));
    }

    site.SiteSettings.TrySetValue(SITESETTING_ATTACHMENT_MAXSIZE, this.AttachmentMaxSize);
    site.SiteSettings.TrySetValue(SITESETTING_INDEXING_PAUSE, this.IndexingPause);

    site.SiteSettings.TrySetValue(SITESETTING_INDEXER_NAME, this.IndexerName);

    site.SiteSettings.TrySetValue(SITESETTING_TIKA_SERVER_URL, this.TikaServerUrl);

    site.SiteSettings.TrySetValue(SITESETTING_BOOST_TITLE, this.Boost.Title);
    site.SiteSettings.TrySetValue(SITESETTING_BOOST_SUMMARY, this.Boost.Summary);
    site.SiteSettings.TrySetValue(SITESETTING_BOOST_CATEGORIES, this.Boost.Categories);
    site.SiteSettings.TrySetValue(SITESETTING_BOOST_KEYWORDS, this.Boost.Keywords);
    site.SiteSettings.TrySetValue(SITESETTING_BOOST_CONTENT, this.Boost.Content);
  }

  private double GetSetting(Site site, string key, double defaultValue)
  {
    if (site.SiteSettings.TryGetValue(key, out string value))
    {
      if (double.TryParse(value, out double parsedValue))
      {
        return parsedValue;
      }
    }
    return defaultValue;
  }

  public static string EncryptApiKey(Site site, string apiKey)
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
  /// Encrypt and encode a password and return the result.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="apiKey"></param>
  /// <returns></returns>
  public static string DecryptApiKey(Site site, string apiKey)
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



