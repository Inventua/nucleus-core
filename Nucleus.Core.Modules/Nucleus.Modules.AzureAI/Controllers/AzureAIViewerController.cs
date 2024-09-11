using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
//using Azure.Identity;
using OpenAI.Chat;

namespace Nucleus.Modules.AzureAI.Controllers;

// The AzureChatMessageContext type is for evaluation purposes only and is subject to change or removal in future updates. AOAI001 must
// be suppressed in order to use it.
#pragma warning disable AOAI001 

[Extension("AzureAI")]
public class AzureAIViewerController : Controller
{
  private Context Context { get; }
  private IPageModuleManager PageModuleManager { get; }

  public AzureAIViewerController(Context Context, IPageModuleManager pageModuleManager)
  {
    this.Context = Context;
    this.PageModuleManager = pageModuleManager;
  }

  [HttpGet]
  public ActionResult Index()
  {
    return View("Viewer", BuildViewModel());
  }

  [HttpPost]
  public async Task<ActionResult> Chat(ViewModels.Viewer viewModel)
  {
    if (string.IsNullOrEmpty(viewModel.Question))
    {
      return BadRequest("You must enter a question.");
    }

    Models.Settings settings = new();

    settings.GetSiteSettings(this.Context.Site);
    settings.GetModuleSettings(this.Context.Module);

    viewModel.History.Add(new() { Question = viewModel.Question });
    viewModel.Question = "";

    if (viewModel.History.Count > settings.OpenAIChatHistoryCount)
    {
      viewModel.History.Remove(viewModel.History.First());
    }

    AzureAI engine = new(this.Context.Site, settings);

    try
    {
      OpenAI.Chat.ChatCompletion completion = await engine.GetCompletion
      (
        viewModel.History
          .Select(history => history.Question)
          .Distinct()
          .ToList()
      );

      AzureChatMessageContext messageContext = completion.GetAzureMessageContext();

      List<AzureChatCitation> distinctCitations = messageContext.Citations.DistinctBy(citation => citation.Url).ToList();

      viewModel.History.Last().DateTime = completion.CreatedAt;
      viewModel.History.Last().Answer = ParseCitations(completion.Content, messageContext.Citations.ToList(), distinctCitations);

      if (viewModel.History.Last().Answer.Contains("<body>") && viewModel.History.Last().Answer.Contains("</body>"))
      {
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(viewModel.History.Last().Answer, "<body>(?<body>.*)<\\/body>", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (match.Success)
        {
          viewModel.History.Last().Answer = match.Groups["body"].Value.Trim();
        }
      }
      else if (viewModel.History.Last().Answer.Trim().StartsWith("```html"))
      {
        viewModel.History.Last().Answer = viewModel.History.Last().Answer
          .Replace("```html", "")
          .Replace("```", "");  
      }
      else
      {
        viewModel.History.Last().Answer = viewModel.History.Last().Answer
          .Replace("\n\n", "<br />")  // replace two CRs with one BR
          .Replace("\n", "<br />");   // make sure we replace any remaining CRs
      }
      //viewModel.History.Last().Answer = string.Join("", completion.Content?.Select(content => content.Text));
      viewModel.History.Last().Citations = distinctCitations;
    }
    catch (ClientResultException ex)
    {
      viewModel.History.Last().Answer = ex.Message;
      System.ClientModel.Primitives.PipelineResponse response = ex.GetRawResponse();
      viewModel.History.Last().Answer += response?.Content.ToString();
      viewModel.History.Last().IsError = true;
    }

    return View("_Answer", viewModel);
  }
   
  private string ParseCitations(IReadOnlyList<ChatMessageContentPart> content, List<AzureChatCitation> originalCitations, List<AzureChatCitation> newCitations)
  {
    string result = "";

    if (content == null) return "";

    foreach (ChatMessageContentPart item in content)
    {
      string text = item.Text;
      System.Text.RegularExpressions.MatchCollection matches = System.Text.RegularExpressions.Regex.Matches(text, "\\[doc(?<citationIndex>[0-9]{1,3})\\]");
      
      foreach (System.Text.RegularExpressions.Match match in matches)
      {
        if (match.Success)
        {
          if (int.TryParse(match.Groups["citationIndex"].Value, out int citationIndex))
          {
            if (originalCitations.Count >= citationIndex)
            {
              int newIndex = newCitations.FindIndex(citation => citation.Url == originalCitations[citationIndex - 1].Url) + 1;
              AzureChatCitation newCitation = newCitations.FirstOrDefault(citation => citation.Url == originalCitations[citationIndex - 1].Url);

              string link = $"<a href='{newCitation.Url}' class='citation-reference' target='_blank' title='{newCitation.Title}'>{newIndex}</a>";
              text = text.Replace($"[doc{citationIndex}]", link);
            }
          }
        }
      }

      result += text;
    }

    return result;
  }

  private ViewModels.Viewer BuildViewModel()
  {
    ViewModels.Viewer viewModel = new();

    viewModel.GetSiteSettings(this.Context.Site);
    viewModel.GetModuleSettings(this.Context.Module);

    return viewModel;
  }
}