using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.Extensions.Shadow.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Extensions.Shadow.Controllers;
[Extension("Shadow")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class ShadowAdminController : Controller
{
  private Context Context { get; }
  private IPageManager PageManager { get; }
  private IPageModuleManager PageModuleManager { get; }

  public ShadowAdminController(Context Context, IPageManager pageManager, IPageModuleManager pageModuleManager)
  {
    this.Context = Context;
    this.PageManager = pageManager;
    this.PageModuleManager = pageModuleManager;
  }

  [HttpGet]
  [HttpPost]
  public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
  {
    return View("Settings", await BuildSettingsViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
  {
    viewModel.SetSettings(this.Context.Module);

    await this.PageModuleManager.SaveSettings(this.Context.Module);

    return Ok();
  }

  private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
  {
    if (viewModel == null)
    {
      viewModel = new();
    }

    if (viewModel.PageId == Guid.Empty)
    {
      viewModel.GetSettings(this.Context.Module);
    }

    viewModel.PageMenu = (await this.PageManager.GetAdminMenu(this.Context.Site, null, ControllerContext.HttpContext.User, 1, true, false, true));

    if (viewModel.PageId != Guid.Empty)
    {
      Page page = await this.PageManager.Get(viewModel.PageId);
      
      if (page != null)
      {
        viewModel.Modules = page.Modules
        .Select(module => new ViewModels.Settings.PageModuleInfo(module.Id, ModuleDisplayName(module)))
        .ToList();
      }
      else
      {
        viewModel.Modules = new();
      }

    }

    return viewModel;
  }

  private string ModuleDisplayName(PageModule module)
  {
    return $"[{module.Pane}] {(String.IsNullOrEmpty(module.Title) ? "No Title" : module.Title)} ({module.ModuleDefinition.FriendlyName})";
  }
}