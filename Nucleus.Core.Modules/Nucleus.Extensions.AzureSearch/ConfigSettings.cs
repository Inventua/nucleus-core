using System;
using System.Collections.Generic;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions.AzureSearch;

public class ConfigSettings
{
  internal const string SITESETTING_SERVER_URL = "azuresearch:server-url";
  internal const string SITESETTING_INDEX_NAME = "azuresearch:indexname";
  internal const string SITESETTING_SERVER_APIKEY = "azuresearch:apikey";
  internal const string SITESETTING_INDEXER_NAME = "azuresearch:indexername";

  internal const string SITESETTING_ATTACHMENT_MAXSIZE = "azuresearch:attachment-maxsize";
  internal const string SITESETTING_INDEXING_PAUSE = "azuresearch:indexing-pause";

  //internal const string SITESETTING_BOOST_TITLE = "azuresearch:boost-title";
  //internal const string SITESETTING_BOOST_SUMMARY = "azuresearch:boost-summary";
  //internal const string SITESETTING_BOOST_CATEGORIES = "azuresearch:boost-categories";
  //internal const string SITESETTING_BOOST_KEYWORDS = "azuresearch:boost-keywords";
  //internal const string SITESETTING_BOOST_CONTENT = "azuresearch:boost-content";

  //internal const string SITESETTING_BOOST_ATTACHMENT_AUTHOR = "azuresearch:boost-attachment-author";
  //internal const string SITESETTING_BOOST_ATTACHMENT_KEYWORDS = "azuresearch:boost-attachment-keywords";
  //internal const string SITESETTING_BOOST_ATTACHMENT_NAME = "azuresearch:boost-attachment-name";
  //internal const string SITESETTING_BOOST_ATTACHMENT_TITLE = "azuresearch:boost-attachment-title";

  public string ServerUrl { get; set; }
  public string IndexName { get; set; }
  public string IndexerName { get; set; }
  public int AttachmentMaxSize { get; set; } = 32;

  public string EncryptedApiKey { get; set; }
  
  public double IndexingPause { get; set; } = 1;

  public Nucleus.Abstractions.Search.SearchQuery.BoostSettings Boost { get; set; } = new();

  public List<string> Indexers { get; set; }

  // This constructor is used by model binding
  public ConfigSettings() { }

  public ConfigSettings(Site site)
  {
    if (site.SiteSettings.TryGetValue(ConfigSettings.SITESETTING_SERVER_URL, out string serverUrl))
    {
      this.ServerUrl = serverUrl;
    }

    if (site.SiteSettings.TryGetValue(ConfigSettings.SITESETTING_INDEX_NAME, out string indexName))
    {
      this.IndexName = indexName;
    }

    if (site.SiteSettings.TryGetValue(ConfigSettings.SITESETTING_INDEXER_NAME, out string indexerName))
    {
      this.IndexerName = indexerName;
    }

    if (site.SiteSettings.TryGetValue(ConfigSettings.SITESETTING_SERVER_APIKEY, out string encryptedApiKey))
    {
      this.EncryptedApiKey = encryptedApiKey;
    }

    if (site.SiteSettings.TryGetValue(ConfigSettings.SITESETTING_ATTACHMENT_MAXSIZE, out string maxSize))
    {
      if (int.TryParse(maxSize, out int attachmentMaxSize))
      {
        this.AttachmentMaxSize = attachmentMaxSize;
      }
    }

    if (site.SiteSettings.TryGetValue(ConfigSettings.SITESETTING_INDEXING_PAUSE, out string indexingPause))
    {
      if (double.TryParse(indexingPause, out double indexingPauseSeconds))
      {
        this.IndexingPause = indexingPauseSeconds;
      }
    }

    //this.Boost.Title = GetSetting(site, SITESETTING_BOOST_TITLE, this.Boost.Title);
    //this.Boost.Summary = GetSetting(site, SITESETTING_BOOST_SUMMARY, this.Boost.Summary);
    //this.Boost.Categories = GetSetting(site, SITESETTING_BOOST_CATEGORIES, this.Boost.Categories);
    //this.Boost.Keywords = GetSetting(site, SITESETTING_BOOST_KEYWORDS, this.Boost.Keywords);
    //this.Boost.Content = GetSetting(site, SITESETTING_BOOST_CONTENT, this.Boost.Content);

    //this.Boost.AttachmentAuthor = GetSetting(site, SITESETTING_BOOST_ATTACHMENT_AUTHOR, this.Boost.AttachmentAuthor);
    //this.Boost.AttachmentKeywords = GetSetting(site, SITESETTING_BOOST_ATTACHMENT_KEYWORDS, this.Boost.AttachmentKeywords);
    //this.Boost.AttachmentName = GetSetting(site, SITESETTING_BOOST_ATTACHMENT_NAME, this.Boost.AttachmentName);
    //this.Boost.AttachmentTitle = GetSetting(site, SITESETTING_BOOST_ATTACHMENT_TITLE, this.Boost.AttachmentTitle);
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

