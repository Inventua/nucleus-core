using System;
using System.Collections.Generic;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions.AzureSearch.Models;

namespace Nucleus.Extensions.AzureSearch;

public class AzureSearchSettings : Models.AzureSearchServiceSettings
{
  private class SettingsKeys
  {
    internal const string SITESETTING_AZURE_SEARCH_SERVICE_URL = "azuresearch:server-url";
    internal const string SITESETTING_AZURE_SEARCH_INDEX_NAME = "azuresearch:indexname";
    internal const string SITESETTING_AZURE_SEARCH_APIKEY = "azuresearch:apikey";
    internal const string SITESETTING_AZURE_SEARCH_SEMANTIC_RANKING_CONFIGURATION_NAME = "azuresearch:semantic-ranking-configuration-name";

    internal const string SITESETTING_AZURE_SEARCH_VECTORIZATION_ENABLED = "azuresearch:vectorization-enabled";

    internal const string SITESETTING_OPENAI_ENDPOINT = "azuresearch:openai-endpoint";
    internal const string SITESETTING_OPENAI_APIKEY = "azuresearch:openai-apikey";
    internal const string SITESETTING_OPENAI_DEPLOYMENTNAME = "azuresearch:openapi-deployment-name";

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
  }

  //public string ServerUrl { get; set; }

  //public string IndexName { get; set; }

  //public string IndexerName { get; set; }

  //public string SemanticConfigurationName { get; set; }

  //public Boolean VectorizationEnabled { get; set; }

  //public string AzureOpenAIEndpoint { get; set; }
  //public string EncryptedAzureOpenAIApiKey { get; set; }
  //public string AzureOpenAIDeploymentName { get; set; }

  //public int AttachmentMaxSize { get; set; } = 32;

  //public string EncryptedApiKey { get; set; }

  //public double IndexingPause { get; set; } = 1;

  //public Nucleus.Abstractions.Search.SearchQuery.BoostSettings Boost { get; set; } = new();

  public OpenAIServiceSettings OpenAIServiceSettings { get; set; } = new();

  // This constructor is used by model binding
  public AzureSearchSettings() { }

  public AzureSearchSettings(Site site)
  {
    if (site.SiteSettings.TryGetValue(SettingsKeys.SITESETTING_AZURE_SEARCH_SERVICE_URL, out string serverUrl))
    {
      this.AzureSearchServiceEndpoint = serverUrl;
    }

    if (site.SiteSettings.TryGetValue(SettingsKeys.SITESETTING_AZURE_SEARCH_INDEX_NAME, out string indexName))
    {
      this.IndexName = indexName;
    }

    if (site.SiteSettings.TryGetValue(SettingsKeys.SITESETTING_AZURE_SEARCH_SEMANTIC_RANKING_CONFIGURATION_NAME, out string semanticConfigurationName))
    {
      this.SemanticConfigurationName = semanticConfigurationName;
    }

    if (site.SiteSettings.TryGetValue(SettingsKeys.SITESETTING_AZURE_SEARCH_VECTORIZATION_ENABLED, out Boolean vectorizationEnabled))
    {
      this.UseVectorSearch = vectorizationEnabled;
    }

    if (site.SiteSettings.TryGetValue(SettingsKeys.SITESETTING_AZURE_SEARCH_APIKEY, out string encryptedApiKey))
    {
      this.AzureSearchServiceEncryptedApiKey = encryptedApiKey;
    }

    if (site.SiteSettings.TryGetValue(SettingsKeys.SITESETTING_ATTACHMENT_MAXSIZE, out string maxSize))
    {
      if (int.TryParse(maxSize, out int attachmentMaxSize))
      {
        this.AttachmentMaxSize = attachmentMaxSize;
      }
    }

    if (site.SiteSettings.TryGetValue(SettingsKeys.SITESETTING_INDEXING_PAUSE, out string indexingPause))
    {
      if (double.TryParse(indexingPause, out double indexingPauseSeconds))
      {
        this.IndexingPause = indexingPauseSeconds;
      }
    }

    if (site.SiteSettings.TryGetValue(SettingsKeys.SITESETTING_OPENAI_ENDPOINT, out string openAIEndPoint))
    {      
      this.OpenAIServiceSettings.AzureOpenAIEndpoint = openAIEndPoint;      
    }

    if (site.SiteSettings.TryGetValue(SettingsKeys.SITESETTING_OPENAI_APIKEY, out string encryptedOpenAIApiKey))
    {
      this.OpenAIServiceSettings.AzureOpenAIEncryptedApiKey = encryptedOpenAIApiKey;
    }

    if (site.SiteSettings.TryGetValue(SettingsKeys.SITESETTING_OPENAI_DEPLOYMENTNAME, out string deploymentName))
    {
      this.OpenAIServiceSettings.AzureOpenAIEmbeddingModelDeploymentName = deploymentName;
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

  public void SaveSettings(Site site, string apiKey, string openAIApiKey)
  {
    if (!this.AzureSearchServiceEndpoint.StartsWith("http"))
    {
      this.AzureSearchServiceEndpoint = "http://" + this.AzureSearchServiceEndpoint;
    }

    site.SiteSettings.TrySetValue(SettingsKeys.SITESETTING_AZURE_SEARCH_SERVICE_URL, this.AzureSearchServiceEndpoint);
    site.SiteSettings.TrySetValue(SettingsKeys.SITESETTING_AZURE_SEARCH_INDEX_NAME, this.IndexName);

    if (apiKey != ViewModels.Settings.DUMMY_APIKEY)
    {
      site.SiteSettings.TrySetValue(SettingsKeys.SITESETTING_AZURE_SEARCH_APIKEY, AzureSearchSettings.Encrypt(site, apiKey));
    }

    site.SiteSettings.TrySetValue(SettingsKeys.SITESETTING_ATTACHMENT_MAXSIZE, this.AttachmentMaxSize);
    site.SiteSettings.TrySetValue(SettingsKeys.SITESETTING_INDEXING_PAUSE, this.IndexingPause);

    site.SiteSettings.TrySetValue(SettingsKeys.SITESETTING_AZURE_SEARCH_SEMANTIC_RANKING_CONFIGURATION_NAME, this.SemanticConfigurationName);
    site.SiteSettings.TrySetValue(SettingsKeys.SITESETTING_AZURE_SEARCH_VECTORIZATION_ENABLED, this.UseVectorSearch);

    site.SiteSettings.TrySetValue(SettingsKeys.SITESETTING_OPENAI_ENDPOINT, this.OpenAIServiceSettings.AzureOpenAIEndpoint);
    
    if (openAIApiKey != ViewModels.Settings.DUMMY_APIKEY)
    {
      site.SiteSettings.TrySetValue(SettingsKeys.SITESETTING_OPENAI_APIKEY, AzureSearchSettings.Encrypt(site, openAIApiKey));
    }

    site.SiteSettings.TrySetValue(SettingsKeys.SITESETTING_OPENAI_DEPLOYMENTNAME, this.OpenAIServiceSettings.AzureOpenAIEmbeddingModelDeploymentName);
  }


  public static string Encrypt(Site site, string encryptedValue)
  {
    if (String.IsNullOrEmpty(encryptedValue))
    {
      return null;
    }

    // Convert string to byte array
    byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes(encryptedValue);

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
  /// <param name="value"></param>
  /// <returns></returns>
  public static string Decrypt(Site site, string value)
  {
    if (String.IsNullOrEmpty(value))
    {
      return null;
    }

    // Convert string to byte array
    byte[] bytesIn = System.Convert.FromBase64String(value);

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

