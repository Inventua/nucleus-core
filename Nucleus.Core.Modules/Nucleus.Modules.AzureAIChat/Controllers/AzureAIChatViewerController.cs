using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.AzureAIChat.Controllers;

[Extension("AzureAIChat")]
public class AzureAIChatViewerController : Controller
{
  private Context Context { get; }
  private IPageModuleManager PageModuleManager { get; }


  public AzureAIChatViewerController(Context Context, IPageModuleManager pageModuleManager)
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
    settings.GetSettings(this.Context.Site, this.Context.Module);
    
    if (viewModel.History.Count > settings.OpenAIChatHistoryCount)
    {
      viewModel.History.Remove(viewModel.History.First());
    }

    AzureAI engine = new(this.Context.Site, settings);
    Models.ChatItem response = await engine.Ask(viewModel.Question, viewModel.History);

    viewModel.History.Add(response);
    if (!response.IsError)
    {
      viewModel.Question = "";
    }   

    return View("_Answer", viewModel);
  } 

  private ViewModels.Viewer BuildViewModel()
  {
    ViewModels.Viewer viewModel = new();

    viewModel.GetSettings(this.Context.Site, this.Context.Module);

    return viewModel;
  }
}