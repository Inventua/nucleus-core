using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Nucleus.Abstractions.Models;
using OpenAI.Chat;

namespace Nucleus.Modules.AzureAI;

// The ChatCompletionOptions.AddDataSource is for evaluation purposes only and is subject to change or removal in future updates. AOAI001 must
// be suppressed in order to use it.
#pragma warning disable AOAI001 

// ChatOptions.Seed is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable OPENAI001 

public class AzureAI
{
  private Site Site { get; }
  private Models.Settings Settings { get; }

  public AzureAI(Site site, Models.Settings settings)
  {
    this.Site = site;
    this.Settings = settings;
  }

  internal async Task<ChatCompletion> GetCompletion(List<string> chatHistory)
  {
    int retryCount = 0;
    
    ChatClient chatClient = BuildChatClient();

    while (true)
    {
      if (retryCount > 0)
      {
        await Task.Delay(TimeSpan.FromSeconds(this.Settings.OpenAIRetryPauseSeconds));
      }

      try
      {
        // Create chat completion request
        return await chatClient.CompleteChatAsync
        (
          messages: chatHistory.Select(history => new UserChatMessage(history)),
          options: BuildChatOptions()
        );
      }
      catch (Exception)
      {
        if (retryCount >= this.Settings.OpenAIMaxRetries)
        {
          throw;
        }
        retryCount++;        
      }
    }
  }

  private ChatClient BuildChatClient()
  {
    AzureKeyCredential credential = new(Models.Settings.DecryptApiKey(this.Site, this.Settings.AzureOpenAIEncryptedApiKey));
    AzureOpenAIClient azureClient = new(new Uri(this.Settings.AzureOpenAIEndpoint), credential);

    ChatClient chatClient = azureClient.GetChatClient(this.Settings.OpenAIChatModelDeploymentName);
    
    return chatClient;
  }

  private ChatCompletionOptions BuildChatOptions()
  {
    // Setup chat completion options with Azure Search data source
    ChatCompletionOptions chatOptions = new();

    chatOptions.MaxTokens = this.Settings.OpenAIMaxTokens;
    chatOptions.Temperature = (float)this.Settings.OpenAITemperature;
    chatOptions.FrequencyPenalty = (float)this.Settings.OpenAIFrequencyPenalty;
    chatOptions.PresencePenalty = (float)this.Settings.OpenAIPresencePenalty;
    chatOptions.Seed = 1;
    chatOptions.TopP = (float)this.Settings.OpenAITopP;    

    // https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/use-your-data?tabs=ai-search%2Ccopilot
    chatOptions.AddDataSource(new AzureSearchChatDataSource()
    {
      Endpoint = new Uri(this.Settings.AzureSearchServiceUrl),
      IndexName = this.Settings.AzureSearchIndexName,
      Authentication = DataSourceAuthentication.FromApiKey(Models.Settings.DecryptApiKey(this.Site, this.Settings.AzureSearchEncryptedApiKey)),
      FieldMappings = BuildFieldMappings(),
      QueryType = DataSourceQueryType.VectorSemanticHybrid,
      SemanticConfiguration = this.Settings.AzureSearchSemanticConfigurationName,
      TopNDocuments = this.Settings.OpenAITopNDocuments,
      InScope = this.Settings.OpenAIInScopeOnly,
      AllowPartialResult = true,
      
      // this (OutputContextFlags) causes an InvalidOperationException "The requested operation requires an element of type 'Object', but the target element has type 'Array'. This
      // is presumably a deserialization bug in the client library, which should get fixed at some point.
      //OutputContextFlags = DataSourceOutputContextFlags.Intent | DataSourceOutputContextFlags.Citations | DataSourceOutputContextFlags.AllRetrievedDocuments,

      Strictness = this.Settings.OpenAIStrictness,
      VectorizationSource = DataSourceVectorizer.FromDeploymentName(this.Settings.AzureOpenAIDeploymentName),
      Filter = BuildPageNumberFilter(),
      RoleInformation = this.Settings.OpenAIRoleInfo
    });

#pragma warning restore AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    return chatOptions;
  }

  private static string BuildPageNumberFilter()
  {
    // We exclude "page 0" from search results, because it is a repeat of the main document (index entry)
    return "PageNumber ne 1";
  }

  private static DataSourceFieldMappings BuildFieldMappings()
  {
    DataSourceFieldMappings mappings = new DataSourceFieldMappings()
    {
      UrlFieldName = "Url",
      TitleFieldName = "Title"
    };

    mappings.ContentFieldNames.Add("Content");

    mappings.VectorFieldNames.Add("ContentVector");
    mappings.VectorFieldNames.Add("TitleVector");
    mappings.VectorFieldNames.Add("SummaryVector");

    return mappings;
  }
}
