using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.Modules.AzureAI.Models;

namespace Nucleus.Modules.AzureAI.Controllers;
[Extension("AzureAI")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class AzureAISettingsController : Controller
{
  private Context Context { get; }
  private IPageModuleManager PageModuleManager { get; }

  public AzureAISettingsController(Context Context, IPageModuleManager pageModuleManager)
  {
    this.Context = Context;
    this.PageModuleManager = pageModuleManager;
  }

  [HttpGet]
  [HttpPost]
  public ActionResult Settings(ViewModels.Settings viewModel)
  {
    return View("Settings", BuildSettingsViewModel(viewModel));
  }

  [HttpPost]
  public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
  {
    viewModel.SetModuleSettings(this.Context.Module);

    await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

    return Ok();
  }

  private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
  {
    if (viewModel == null)
    {
      viewModel = new();
    }
    
    viewModel.GetSiteSettings(this.Context.Site);
    viewModel.GetModuleSettings(this.Context.Module);

    return viewModel;
  }
}