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
using Nucleus.Extensions.TikaServerConverter.Models;

namespace Nucleus.Extensions.TikaServerConverter.Controllers;
[Extension("TikaServerConverter")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
public class TikaServerConverterSettingsController : Controller
{
  private Context Context { get; }
  private ISiteManager SiteManager { get; }

  public TikaServerConverterSettingsController(Context Context, ISiteManager siteManager)
  {
    this.Context = Context;
    this.SiteManager = siteManager;
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
    viewModel.SetSettings(this.Context.Site);

    await this.SiteManager.Save(this.Context.Site);

    return Ok();
  }

  private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
  {
    if (viewModel == null)
    {
      viewModel = new();
    }

    viewModel.GetSettings(this.Context.Site);

    return viewModel;
  }
}