using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.CopilotClient.Models;
using Nucleus.Extensions;
using System.Net.Http.Json;
using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json.Linq;
using Nucleus.Extensions.Authorization;
using Newtonsoft.Json;
using static Nucleus.CopilotClient.ViewModels.Viewer;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;

namespace Nucleus.CopilotClient.Controllers;

[Extension("CopilotClient")]
public class CopilotClientViewerController : Controller
{
  private Context Context { get; }
  private IHttpClientFactory HttpClientFactory { get; }

  private static DirectLineToken ActiveDirectLineToken { get; set; }
  private static DateTime DirectLineTokenExpiry { get; set; }

  public CopilotClientViewerController(Context Context, IHttpClientFactory httpClientFactory)
  {
    this.Context = Context;
    this.HttpClientFactory = httpClientFactory;
  }

  [HttpGet]
  public async Task<ActionResult> Index()
  {
    ViewModels.Viewer viewModel = BuildViewModel(null);

    await StartConversation(viewModel);

    return View("Viewer", viewModel);
  }

  [HttpPost]
  public async Task<ActionResult> PostMessage(ViewModels.Viewer viewModel)
  {
    viewModel = BuildViewModel(viewModel);

    await SendMessage(viewModel);

    return NoContent();
  }

  ////private async Task<TResult> TryCopilotCall<TResult>(string tokenEndpoint, Func<TResult> action)
  ////{
  ////  DirectLineToken token = await GetDirectLineToken(tokenEndpoint);

  ////  if (token==null) return null;

  ////  using (var directLineClient = new DirectLineClient(token.Token))
  ////  {

  ////    try
  ////    {
  ////      return action.Invoke();
  ////    }
  ////    catch (Microsoft.Rest.TransientFaultHandling.HttpRequestWithStatusException ex)
  ////    {
  ////      //: 'Response status code indicates server error: 403 (Forbidden).'
  ////      if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
  ////      {
  ////        ActiveDirectLineToken = null;
  ////        token = await GetDirectLineToken(tokenEndpoint);
  ////        return action.Invoke();
  ////      }
  ////      else
  ////      {
  ////        throw;
  ////      }
  ////    }
  ////  }
  ////}


  [HttpPost]
  public async Task<ActionResult> ReadResponses(ViewModels.Viewer viewModel)
  {
    ActivitySet response;

    viewModel = BuildViewModel(viewModel);
    DirectLineToken token = await GetDirectLineToken(viewModel.TokenEndpoint);

    if (token != null)
    {
      viewModel.IsConfigured = true;
      using (var directLineClient = new DirectLineClient(token.Token))
      {
        // To get the first response set string watermark = null
        // More information about watermark is available at
        // https://learn.microsoft.com/azure/bot-service/rest-api/bot-framework-rest-direct-line-1-1-receive-messages?view=azure-bot-service-4.0

        try
        {
          // response from bot is of type Microsoft.Bot.Connector.DirectLine.ActivitySet
          response = await directLineClient.Conversations.GetActivitiesAsync
          (
            viewModel.ConversationId,
            String.IsNullOrEmpty(viewModel.Watermark) ? null : viewModel.Watermark
          );
        }
        catch (Microsoft.Rest.TransientFaultHandling.HttpRequestWithStatusException ex)
        {
          // renew token if we get a response status code 403 (Forbidden)
          if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
          {
            ActiveDirectLineToken = null;
            token = await GetDirectLineToken(viewModel.TokenEndpoint);
            response = await directLineClient.Conversations.GetActivitiesAsync
            (
              viewModel.ConversationId,
              String.IsNullOrEmpty(viewModel.Watermark) ? null : viewModel.Watermark
            );
          }
          else
          {
            throw;
          }
        }

        if (response == null || response?.Activities == null)
        {
          return NoContent();
        }
        else
        {

          // return bot responses
          return Json(new ViewModels.Viewer.Message()
          {
            Watermark = response?.Watermark,
            Responses = response.Activities.Select(response => new ViewModels.Viewer.Response()
            {
              Type = (response.ReplyToId == null ? "copilot-question" : "copilot-answer"),
              Text = RenderText(response)
            }),
            Citations = RenderCitations(response.Activities)
          });
        }
      }
    }
    else
    {
      return NoContent();
    }
  }

  private string RenderText(Activity value)
  {
    string result = "<div class='copilot-icon'></div><div class='copilot-content'>" + value.Text.ToHtml("text/markdown") + "</div>";

    result = System.Text.RegularExpressions.Regex.Replace
    (
      result,
      "a href=\"(?<href>[^\"]*)\"",
      new System.Text.RegularExpressions.MatchEvaluator(match => ReplaceCitationLinks(match, value))
    );

    return result;
  }

  private string ReplaceCitationLinks(System.Text.RegularExpressions.Match match, Activity value)
  {
    string href = match.Groups["href"].Value;

    foreach (Entity entity in value.Entities)
    {
      string entityCitationId = entity.Properties["@id"]?.ToString();
      if (entityCitationId == href)
      {
        if (!href.StartsWith("http"))
        {
          string entityCitationName = entity.Properties["name"]?.ToString();
          string entityCitationText = entity.Properties["text"]?.ToString();

          return match.Value.Replace(href, $"#{entityCitationId}") + $" data-citation='{entityCitationId}' ";
        }
        else
        {
          return match.Value + " target='blank'";
        }
      }
    }

    return match.Value;
  }

  private IEnumerable<ViewModels.Viewer.Citation> RenderCitations(IList<Activity> values)
  {
    List<ViewModels.Viewer.Citation> results = new();

    foreach (Activity activity in values)
    {
      if (activity.Entities != null)
      {
        foreach (Entity entity in activity.Entities)
        {
          string entityCitationId = "#" + entity.Properties["@id"]?.ToString();
          string entityCitationName = entity.Properties["name"]?.ToString();
          string entityCitationText = entity.Properties["text"]?.ToString();

          results.Add(new() { Id = entityCitationId, Name = entityCitationName, Text = entityCitationText });
        }
      }
    }

    return results;
  }

  private ViewModels.Viewer BuildViewModel(ViewModels.Viewer viewModel)
  {
    if (viewModel == null)
    {
      viewModel = new();
    }

    viewModel.GetSettings(this.Context.Module);
    viewModel.ModuleId = this.Context.Module.Id;

    return viewModel;
  }

  private async Task<DirectLineToken> GetDirectLineToken(string tokenEndpoint)
  {
    if (ActiveDirectLineToken == null)
    {
      // get a new token
      HttpClient client = this.HttpClientFactory.CreateClient();

      if (String.IsNullOrEmpty(tokenEndpoint))
      {
        return null;
      }

      ActiveDirectLineToken = await client.GetFromJsonAsync<DirectLineToken>(tokenEndpoint);
      DirectLineTokenExpiry = DateTime.UtcNow.AddMinutes(ActiveDirectLineToken.Expires_in);
    }
    else if (DateTime.UtcNow > DirectLineTokenExpiry.Subtract(TimeSpan.FromMinutes(5)))
    {
      // renew an expired token
      HttpClient client = this.HttpClientFactory.CreateClient();

      if (String.IsNullOrEmpty(tokenEndpoint))
      {
        return null;
      }

      string refreshTokenUri = new Uri(new Uri(tokenEndpoint + (tokenEndpoint.EndsWith("/") ? "" : "/")), "v3/directline/tokens/refresh").ToString();

      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, refreshTokenUri);
      request.Headers.Authorization = new("Bearer", ActiveDirectLineToken.Token);
      HttpResponseMessage response = await client.SendAsync(request);
      ActiveDirectLineToken = JsonConvert.DeserializeObject<DirectLineToken>(await response.Content.ReadAsStringAsync());
      DirectLineTokenExpiry = DateTime.UtcNow.AddMinutes(ActiveDirectLineToken.Expires_in);
    }

    return ActiveDirectLineToken;
  }

  private async Task StartConversation(ViewModels.Viewer viewModel)
  {
    DirectLineToken token = await GetDirectLineToken(viewModel.TokenEndpoint);

    if (token != null)
    {
      viewModel.IsConfigured = true;
      using (var directLineClient = new DirectLineClient(token.Token))
      {
        Conversation conversation = await directLineClient.Conversations.StartConversationAsync();
        viewModel.ConversationId = conversation.ConversationId;
      }
    }
  }

  private async Task SendMessage(ViewModels.Viewer viewModel)
  {
    DirectLineToken token = await GetDirectLineToken(viewModel.TokenEndpoint);

    if (token != null)
    {
      viewModel.IsConfigured = true;
      using (var directLineClient = new DirectLineClient(token.Token))
      {
        // Send user message using directlineClient
        // Payload is a Microsoft.Bot.Connector.DirectLine.Activity
        ResourceResponse response = await directLineClient.Conversations.PostActivityAsync(viewModel.ConversationId, new Activity()
        {
          Type = ActivityTypes.Message,
          From = new ChannelAccount { Id = User.GetUserId().ToString(), Name = User.Identity.Name },
          Text = viewModel.Question,
          TextFormat = "plain",
          Locale = "en-us",
        });
      }
    }
  }

}