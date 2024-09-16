using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using Nucleus.Modules.AzureAIChat.Models;
using OpenAI.Chat;
using System.Text.RegularExpressions;

namespace Nucleus.Modules.AzureAIChat;

// The ChatCompletionOptions.AddDataSource is for evaluation purposes only and is subject to change or removal in future updates. AOAI001 must
// be suppressed in order to use it.
#pragma warning disable AOAI001 

// ChatOptions.Seed is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable OPENAI001 

public class AzureAI
{
  private Site Site { get; }
  private Models.Settings Settings { get; }

  private readonly char[] MARKDOWN_CHARACTERS = ['#', '*', '-'];
  private readonly char[] HTML_CHARACTERS = ['>', '<'];

  public AzureAI(Site site, Models.Settings settings)
  {
    this.Site = site;
    this.Settings = settings;
  }

  /// <summary>
  /// Submit the specified question and history to the chat completion service and return a parsed response.
  /// </summary>
  /// <param name="question"></param>
  /// <param name="history"></param>
  /// <returns></returns>
  public async Task<ChatItem> Ask(string question, List<ChatItem> history)
  {
    try
    {
      OpenAI.Chat.ChatCompletion completion = await this.GetCompletion
      (
        history
          .Select(history => history.Question).Append(question)
          .Distinct()
          .ToList()
      );

      return this.ParseContent(question, completion);
    }
    catch (ClientResultException ex)
    {
      ChatItem errorItem = new()
      {
        Question = question,
        Answer = ex.Message,
        IsError = true
      };

      System.ClientModel.Primitives.PipelineResponse response = ex.GetRawResponse();
      errorItem.Answer += response?.Content.ToString();

      return errorItem;
    }
  }

  /// <summary>
  /// Submit a request to the chat completion service and return the response.
  /// </summary>
  /// <param name="chatHistory"></param>
  /// <returns></returns>
  private async Task<ChatCompletion> GetCompletion(List<string> chatHistory)
  {
    int retryCount = 0;

    ChatClient chatClient = BuildChatClient();

    while (true)
    {
      try
      {
        //List<ChatMessage> messages = [new SystemChatMessage(this.Settings.OpenAIRoleInfo)];
        //messages.AddRange(chatHistory.Select(history => new UserChatMessage(history)));

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
        await Task.Delay(TimeSpan.FromSeconds(this.Settings.OpenAIRetryPauseSeconds));
      }
    }
  }

  /// <summary>
  /// Parse a chat completion response.  Convert the content to HTML, merge duplicate citations and remove citations that are not referred
  /// to in the response.
  /// </summary>
  /// <param name="question"></param>
  /// <param name="completion"></param>
  /// <returns></returns>
  private ChatItem ParseContent(string question, ChatCompletion completion)
  {
    AzureChatMessageContext messageContext = completion.GetAzureMessageContext();
    List<AzureChatCitation> originalCitations = messageContext.Citations.ToList();
    List<AzureChatCitation> referencedCitations = [];

    string answer = "";

    foreach (ChatMessageContentPart item in completion.Content)
    {
      string text = ConvertToHtml(item.Text);

      foreach (Match match in Regex.Matches(text, "\\[doc(?<citationIndex>[0-9]{1,3})\\]").Where(match => match.Success))
      {
        if (int.TryParse(match.Groups["citationIndex"].Value, out int citationIndex))
        {
          if (originalCitations.Count >= citationIndex)
          {
            AzureChatCitation referencedCitation = null;

            if (originalCitations[citationIndex - 1].Url == null)
            {
              // the API from September 2024 seems to return citations with a null url - which we can't use as a link. Look for a citation for the parent document,
              // which will have an ID that is "in the middle" of the child document ID
              referencedCitation = originalCitations.Where(citation => citation.Url != null && originalCitations[citationIndex - 1].Filepath.Contains(citation.Filepath)).FirstOrDefault();
            }
            else
            {
              referencedCitation = originalCitations.Where(citation => citation.Url == originalCitations[citationIndex - 1].Url).FirstOrDefault();
            }

            if (referencedCitation == null)
            {
              // can't find a good alternative citation, use the original one
              referencedCitation = originalCitations[citationIndex - 1];
            }

            if (referencedCitation != null)
            {
              int referencedCitationIndex;

              if (!referencedCitations.Any(citation => citation.Url == referencedCitation.Url))
              {
                referencedCitations.Add(referencedCitation);
                referencedCitationIndex = referencedCitations.FindIndex(citation => citation == referencedCitation);
              }
              else
              {
                referencedCitationIndex = referencedCitations.FindIndex(citation => citation.Url == referencedCitation.Url);
              }

              // replace the citation if we found a better match
              if (citationIndex != referencedCitationIndex)
              {
                text = text.Replace($"[doc{citationIndex}]", $"[doc{referencedCitationIndex}]");
              }
            }
          }
        }
      }

      // after we have swapped "good" citations for "bad" ones, if the text has links to both, we can have duplicates - replace duplicates
      for (int citationIndex = 0; citationIndex < originalCitations.Count; citationIndex++)
      {
        while (text.Contains($"[doc{citationIndex}][doc{citationIndex}]"))
        {
          text = text.Replace($"[doc{citationIndex}][doc{citationIndex}]", $"[doc{citationIndex}]");
        }
      }

      // run through the [docN] instances again, and replace with a <a> element
      for (int citationIndex = 0; citationIndex < referencedCitations.Count; citationIndex++)
      {
          AzureChatCitation referencedCitation = referencedCitations[citationIndex];
          string link = $"<a href='{referencedCitation.Url}' class='citation-reference' target='_blank' title='{referencedCitation.Title}'>{citationIndex + 1}</a>";

          text = text.Replace($"[doc{citationIndex}]", link);
      }

      answer += text;
    }

    return new()
    {
      DateTime = completion.CreatedAt,
      Question = question,
      Answer = answer,
      Citations = referencedCitations
    };

  }

  /// <summary>
  /// Detect whether the content specified by <paramref name="value"/> is in Markdown, HTML or plain-text format, and convert it to HTML.
  /// </summary>
  /// <param name="value"></param>
  /// <returns></returns>
  /// <remarks>
  /// The results from this function are not 100% reliable, as we use a fairly simple method to detect the content type. Markdown 
  /// can contain HTML, so 
  /// </remarks>
  private string ConvertToHtml(string value)
  {
    // try to determine the format of the response
    string contentType = "text/plain";

    float htmlLikelihood = value.Count(ch => HTML_CHARACTERS.Contains(ch)) / (float)value.Length * 100;
    float markdownLikelihood = value.Count(ch => MARKDOWN_CHARACTERS.Contains(ch)) / (float)value.Length * 100;

    if (markdownLikelihood > 0.5 && markdownLikelihood > htmlLikelihood)
    {
      // if there are more than .5% markdown characters & more markdown characters than HTML characters
      contentType = "text/markdown";
    }
    else if (htmlLikelihood > 0.5 && htmlLikelihood > markdownLikelihood)
    {
      // if there are more than .5% HTML characters & more HTML characters than markdown characters
      contentType = "text/html";
    }

    string htmlText = value.ToHtml(contentType);

    if (htmlText.Contains("<body>") && htmlText.Contains("</body>"))
    {
      System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(htmlText, "<body>(?<body>.*)<\\/body>", System.Text.RegularExpressions.RegexOptions.Singleline);
      if (match.Success)
      {
        htmlText = match.Groups["body"].Value.Trim();
      }
    }
    else if (htmlText.Trim().StartsWith("```html"))
    {
      htmlText = htmlText
        .Replace("```html", "")
        .Replace("```", "");
    }

    return htmlText;
  }

  /// <summary>
  /// Return a chat client
  /// </summary>
  /// <returns></returns>
  private ChatClient BuildChatClient()
  {
    AzureKeyCredential credential = new(Models.Settings.DecryptApiKey(this.Site, this.Settings.AzureOpenAIEncryptedApiKey));
    AzureOpenAIClient azureClient = new(new Uri(this.Settings.AzureOpenAIEndpoint), credential);

    ChatClient chatClient = azureClient.GetChatClient(this.Settings.OpenAIChatModelDeploymentName);

    return chatClient;
  }

  /// <summary>
  /// Setup chat completion options with Azure Search data source
  /// </summary>
  /// <returns></returns>
  private ChatCompletionOptions BuildChatOptions()
  {
    ChatCompletionOptions chatOptions = new()
    {
      MaxTokens = this.Settings.OpenAIMaxTokens,
      Temperature = (float)this.Settings.OpenAITemperature,
      FrequencyPenalty = (float)this.Settings.OpenAIFrequencyPenalty,
      PresencePenalty = (float)this.Settings.OpenAIPresencePenalty,
      Seed = 100,
      TopP = (float)this.Settings.OpenAITopP
    };

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
      AllowPartialResult = false,

      // this (OutputContextFlags) causes an InvalidOperationException "The requested operation requires an element of type 'Object', but the target element has type 'Array'. This
      // is presumably a deserialization bug in the client library, which should get fixed at some point.
      //OutputContextFlags = DataSourceOutputContextFlags.Intent | DataSourceOutputContextFlags.Citations | DataSourceOutputContextFlags.AllRetrievedDocuments,

      Strictness = this.Settings.OpenAIStrictness,
      VectorizationSource = DataSourceVectorizer.FromDeploymentName(this.Settings.AzureOpenAIDeploymentName),
      Filter = BuildPageNumberFilter(),
      RoleInformation = this.Settings.OpenAIRoleInfo
    });

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
      TitleFieldName = "Title",
      FilepathFieldName = "Id"
    };

    mappings.ContentFieldNames.Add("Content");

    mappings.VectorFieldNames.Add("ContentVector");
    mappings.VectorFieldNames.Add("TitleVector");
    mappings.VectorFieldNames.Add("SummaryVector");

    return mappings;
  }
}
