using System;
using System.Collections.Generic;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.AzureAIChat.Models;

public class Settings
{
  // settings which belong to this module
  internal const string SITESETTING_OPENAI_CHAT_MODEL_DEPLOYMENT_NAME = "azureopenai:chat-model-deployment-name";

  internal const string SITESETTING_OPENAI_CHAT_HISTORY_COUNT = "azureopenai:chat-history-count";

  internal const string SITESETTING_OPENAI_CHAT_MAX_TOKENS = "azureopenai:chat-max-tokens";
  internal const string SITESETTING_OPENAI_CHAT_MAX_RETRIES = "azureopenai:chat-max-retries";
  internal const string SITESETTING_OPENAI_CHAT_RETRY_PAUSE_SECONDS = "azureopenai:chat-retry-pause-seconds";
  internal const string SITESETTING_OPENAI_CHAT_ROLE_INFO = "azureopenai:chat-role-info";

  internal const string SITESETTING_OPENAI_STRICTNESS = "azureopenai:chat-strictness";
  internal const string SITESETTING_OPENAI_TEMPERATURE = "azureopenai:chat-temperature";
  internal const string SITESETTING_OPENAI_TOP_P = "azureopenai:chat-top-p";
  internal const string SITESETTING_OPENAI_FREQUENCY_PENALTY = "azureopenai:chat-frequency-penalty";
  internal const string SITESETTING_OPENAI_PRESENCE_PENALTY = "azureopenai:chat-presence-penalty";

  internal const string SITESETTING_OPENAI_TOP_N_DOCUMENTS = "azureopenai:chat-top-n-documents";
  internal const string SITESETTING_OPENAI_INSCOPE_ONLY = "azureopenai:chat-in-scope-only";

  // settings that are shared with Nucleus.Extensions.AzureSearch
  internal const string SITESETTING_AZURE_SEARCH_SERVICE_URL = "azuresearch:server-url";
  internal const string SITESETTING_AZURE_SEARCH_INDEX_NAME = "azuresearch:indexname";
  internal const string SITESETTING_AZURE_SEARCH_APIKEY = "azuresearch:apikey";
  internal const string SITESETTING_AZURE_SEARCH_SEMANTIC_RANKING_CONFIGURATION_NAME = "azuresearch:semantic-ranking-configuration-name";

  internal const string SITESETTING_AZURE_SEARCH__VECTORIZATION_ENABLED = "azuresearch:vectorization-enabled";

  internal const string SITESETTING_OPENAI_ENDPOINT = "azuresearch:openai-endpoint";
  internal const string SITESETTING_OPENAI_APIKEY = "azuresearch:openai-apikey";
  internal const string SITESETTING_OPENAI_DEPLOYMENTNAME = "azuresearch:openapi-deployment-name";

  public string OpenAIChatModelDeploymentName { get; set; }

  public Boolean OpenAIInScopeOnly { get; set; } = true;

  public int OpenAITopNDocuments { get; set; } = 8;
  
  public int OpenAIStrictness { get; set; } = 3;

  public double OpenAITemperature { get; set; } = 0;
  public double OpenAITopP { get; set; } = 0;

  public double OpenAIFrequencyPenalty { get; set; } = 0;
  public double OpenAIPresencePenalty { get; set; } = 0;

  public int OpenAIChatHistoryCount { get; set; } = 3;

  public int OpenAIMaxTokens { get; set; } = 2500;

  public int OpenAIMaxRetries { get; set; } = 1;

  public int OpenAIRetryPauseSeconds { get; set; } = 10;

  public string OpenAIRoleInfo { get; set; }= "You are a helpful assistant. Please generate citations to retrieved documents for every claim in your answer. If the user question cannot be answered using retrieved documents, please explain the reasoning behind why documents are relevant to user queries.";

  public string AzureSearchServiceUrl { get; set; } 

  public string AzureSearchIndexName { get; set; }

  public string AzureSearchEncryptedApiKey { get; set; }

  public string AzureSearchSemanticConfigurationName { get; set; }

  public bool AzureSearchVectorizationEnabled { get; set; }

  public string AzureOpenAIEndpoint { get; set; }

  public string AzureOpenAIEncryptedApiKey { get; set; }

  public string AzureOpenAIDeploymentName { get; set; }

  // This constructor is used by model binding
  public Settings() { }

  public void GetSettings(Site site, PageModule module)
  {
    GetSiteSettings(site);
    GetModuleSettings(module);
  }

  public void GetSiteSettings(Site site)
  {
    // shared settings
    if (site.SiteSettings.TryGetValue(SITESETTING_AZURE_SEARCH_SERVICE_URL, out string serverUrl))
    {
      this.AzureSearchServiceUrl = serverUrl;
    }

    if (site.SiteSettings.TryGetValue(SITESETTING_AZURE_SEARCH_INDEX_NAME, out string indexName))
    {
      this.AzureSearchIndexName = indexName;
    }

    if (site.SiteSettings.TryGetValue(SITESETTING_AZURE_SEARCH_SEMANTIC_RANKING_CONFIGURATION_NAME, out string semanticConfigurationName))
    {
      this.AzureSearchSemanticConfigurationName = semanticConfigurationName;
    }

    if (site.SiteSettings.TryGetValue(SITESETTING_AZURE_SEARCH__VECTORIZATION_ENABLED, out bool vectorizationEnabled))
    {
      this.AzureSearchVectorizationEnabled = vectorizationEnabled;
    }

    if (site.SiteSettings.TryGetValue(SITESETTING_AZURE_SEARCH_APIKEY, out string encryptedApiKey))
    {
      this.AzureSearchEncryptedApiKey = encryptedApiKey;
    }

    if (site.SiteSettings.TryGetValue(SITESETTING_OPENAI_ENDPOINT, out string openAIEndPoint))
    {
      this.AzureOpenAIEndpoint = openAIEndPoint;
    }

    if (site.SiteSettings.TryGetValue(SITESETTING_OPENAI_APIKEY, out string encryptedOpenAIApiKey))
    {
      this.AzureOpenAIEncryptedApiKey = encryptedOpenAIApiKey;
    }

    if (site.SiteSettings.TryGetValue(SITESETTING_OPENAI_DEPLOYMENTNAME, out string deploymentName))
    {
      this.AzureOpenAIDeploymentName = deploymentName;
    }

  }

  public void GetModuleSettings(PageModule module)
  {
    // settings for this module    
    this.OpenAIChatModelDeploymentName = module.ModuleSettings.Get(SITESETTING_OPENAI_CHAT_MODEL_DEPLOYMENT_NAME, "");
    this.OpenAIChatHistoryCount = module.ModuleSettings.Get(SITESETTING_OPENAI_CHAT_HISTORY_COUNT, this.OpenAIChatHistoryCount);

    this.OpenAIMaxTokens = module.ModuleSettings.Get(SITESETTING_OPENAI_CHAT_MAX_TOKENS, this.OpenAIMaxTokens);
    this.OpenAIMaxRetries = module.ModuleSettings.Get(SITESETTING_OPENAI_CHAT_MAX_RETRIES, this.OpenAIMaxRetries);

    this.OpenAIRetryPauseSeconds = module.ModuleSettings.Get(SITESETTING_OPENAI_CHAT_RETRY_PAUSE_SECONDS, this.OpenAIRetryPauseSeconds);
    this.OpenAIRoleInfo = module.ModuleSettings.Get(SITESETTING_OPENAI_CHAT_ROLE_INFO, this.OpenAIRoleInfo);

    this.OpenAITopNDocuments = module.ModuleSettings.Get(SITESETTING_OPENAI_TOP_N_DOCUMENTS, this.OpenAITopNDocuments);
    this.OpenAIInScopeOnly = module.ModuleSettings.Get(SITESETTING_OPENAI_INSCOPE_ONLY, this.OpenAIInScopeOnly);
    
    this.OpenAIStrictness = module.ModuleSettings.Get(SITESETTING_OPENAI_STRICTNESS, this.OpenAIStrictness);
    this.OpenAITemperature = module.ModuleSettings.Get(SITESETTING_OPENAI_TEMPERATURE, this.OpenAITemperature);
    this.OpenAITopP = module.ModuleSettings.Get(SITESETTING_OPENAI_TOP_P, this.OpenAITopP);

    this.OpenAIFrequencyPenalty = module.ModuleSettings.Get(SITESETTING_OPENAI_FREQUENCY_PENALTY, this.OpenAIFrequencyPenalty);
    this.OpenAIPresencePenalty = module.ModuleSettings.Get(SITESETTING_OPENAI_PRESENCE_PENALTY, this.OpenAIPresencePenalty);

  }

  public void SetModuleSettings(PageModule module)
  {
    // only set module settings, not the site settings that are shared with (and set by) Nucleus.Extensions.AzureSearch
    module.ModuleSettings.Set(SITESETTING_OPENAI_CHAT_MODEL_DEPLOYMENT_NAME, this.OpenAIChatModelDeploymentName);
    module.ModuleSettings.Set(SITESETTING_OPENAI_CHAT_HISTORY_COUNT, this.OpenAIChatHistoryCount);

    module.ModuleSettings.Set(SITESETTING_OPENAI_CHAT_MAX_TOKENS, this.OpenAIMaxTokens);
    module.ModuleSettings.Set(SITESETTING_OPENAI_CHAT_MAX_RETRIES, this.OpenAIMaxRetries);

    module.ModuleSettings.Set(SITESETTING_OPENAI_CHAT_RETRY_PAUSE_SECONDS, this.OpenAIRetryPauseSeconds);
    module.ModuleSettings.Set(SITESETTING_OPENAI_CHAT_ROLE_INFO, this.OpenAIRoleInfo);

    module.ModuleSettings.Set(SITESETTING_OPENAI_TOP_N_DOCUMENTS, this.OpenAITopNDocuments);
    module.ModuleSettings.Set(SITESETTING_OPENAI_INSCOPE_ONLY, this.OpenAIInScopeOnly);

    module.ModuleSettings.Set(SITESETTING_OPENAI_STRICTNESS, this.OpenAIStrictness);
    module.ModuleSettings.Set(SITESETTING_OPENAI_TEMPERATURE, this.OpenAITemperature);
    module.ModuleSettings.Set(SITESETTING_OPENAI_TOP_P, this.OpenAITopP);

    module.ModuleSettings.Set(SITESETTING_OPENAI_FREQUENCY_PENALTY, this.OpenAIFrequencyPenalty);
    module.ModuleSettings.Set(SITESETTING_OPENAI_PRESENCE_PENALTY, this.OpenAIPresencePenalty);
  }

  public static string EncryptApiKey(Site site, string apiKey)
  {
    if (string.IsNullOrEmpty(apiKey))
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
    return Convert.ToBase64String(bytesOut);
  }

  /// <summary>
  /// Encrypt and encode a password and return the result.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="apiKey"></param>
  /// <returns></returns>
  public static string DecryptApiKey(Site site, string apiKey)
  {
    if (string.IsNullOrEmpty(apiKey))
    {
      return null;
    }

    // Convert string to byte array
    byte[] bytesIn = Convert.FromBase64String(apiKey);

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

