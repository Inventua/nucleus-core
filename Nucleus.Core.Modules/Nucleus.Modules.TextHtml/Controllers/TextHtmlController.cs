using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.TextHtml.Controllers;

[Extension("TextHtml")]
public class TextHtmlController : Controller
{
  private Context Context { get; }
  private IPageModuleManager PageModuleManager { get; }
  private IContentManager ContentManager { get; }

  public TextHtmlController(Context Context, IPageModuleManager pageModuleManager, IContentManager contentManager)
  {
    this.Context = Context;
    this.PageModuleManager = pageModuleManager;
    this.ContentManager = contentManager;
  }

  [HttpGet]
  public async Task<ActionResult> Index()
  {
    return View("Viewer", await BuildViewModel());
  }

  [HttpPost]
  public async Task<ActionResult> Edit()
  {
    return View("Settings", await BuildSettingsViewModel());
  }

  [HttpPost]
  public ActionResult ChangeFormat(ViewModels.Settings viewModel, string format)
  {
    viewModel.Content.ConvertTo(format);
    this.ModelState.Clear();

    return View("Settings", viewModel);
  }

  [HttpPost]
  public async Task<ActionResult> Save(ViewModels.Settings viewModel)
  {
    // Text/Html only ever has one item
    viewModel.Content.SortOrder = 10;

    // Text/Html is a special case, it has a title control in its editor
    PageModule module = await this.PageModuleManager.Get(this.Context.Module.Id);
    module.Title = viewModel.Title;
    await this.PageModuleManager.Save(this.Context.Page, module);

    // save updated content
    await this.ContentManager.Save(this.Context.Module, viewModel.Content);

    return Ok();
  }


  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> UpdateContent(Guid? id, string value)
  {
    // apply inline content edits. The inline editor format is always HTML, so we need to convert the value back to the original content type before saving
    Content content = id == null ? new Content() { SortOrder = 10 } : await this.ContentManager.Get(id.Value);
    string originalContentType = content.ContentType;

    content.Value = value;
    content.ContentType = "text/html";

    content.ConvertTo(originalContentType);

    await this.ContentManager.Save(this.Context.Module, content);

    return Ok();
  }
  private async Task<ViewModels.Viewer> BuildViewModel()
  {
    ViewModels.Viewer viewModel = new ViewModels.Viewer();

    if (this.Context.Module != null)
    {
      viewModel.Content = (await this.ContentManager.List(this.Context.Module)).FirstOrDefault();
    }

    return viewModel;
  }

  private async Task<ViewModels.Settings> BuildSettingsViewModel()
  {
    ViewModels.Settings viewModel = new();

    if (this.Context.Module != null)
    {
      viewModel.ModuleId = this.Context.Module.Id;
      viewModel.Title = this.Context.Module.Title;

      List<Content> contents = await this.ContentManager.List(this.Context.Module);

      // Text/Html only ever has one item
      viewModel.Content = contents.FirstOrDefault();
    }

    return viewModel;
  }

}
