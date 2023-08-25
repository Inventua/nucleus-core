using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.Modules.IFrame.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Modules.IFrame.Controllers;
[Extension("IFrame")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class IFrameSettingsController : Controller
{
  private Context Context { get; }
  private IPageModuleManager PageModuleManager { get; }

  public IFrameSettingsController(Context Context, IPageModuleManager pageModuleManager)
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
    viewModel.SetSettings(this.Context.Module);

    await this.PageModuleManager.SaveSettings(this.Context.Module);

    return Ok();
  }

  private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
  {
    if (viewModel == null)
    {
      viewModel = new();
    }

    viewModel.GetSettings(this.Context.Module);

    return viewModel;
  }
}