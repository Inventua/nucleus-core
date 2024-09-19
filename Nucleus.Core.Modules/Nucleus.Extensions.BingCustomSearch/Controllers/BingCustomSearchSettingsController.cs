using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;
using System.Threading.Tasks;

namespace Nucleus.Extensions.BingCustomSearch.Controllers;

[Extension("BingCustomSearch")]
[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
public class BingCustomSearchSettingsController : Controller
{
  private Context Context { get; }
  private ISiteManager SiteManager { get; }

  public BingCustomSearchSettingsController(Context Context, ISiteManager siteManager)
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
    if (String.IsNullOrEmpty(viewModel.ApiKey))
    {
      viewModel.SetApiKey(this.Context.Site, "");
    }
    else if (viewModel.ApiKey != ViewModels.Settings.DUMMY_API_KEY)
    {
      viewModel.SetApiKey(this.Context.Site, viewModel.ApiKey);
    }
    else
    {
      viewModel.ReadEncryptedApiKey(this.Context.Site);
    }

    if (!ModelState.IsValid)
    {
      return BadRequest(ModelState);
    }

    viewModel.SetSettings(this.Context.Site);

    await this.SiteManager.Save(this.Context.Site);

    return Ok();
  }

  private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
  {
    viewModel ??= new();

    viewModel.GetSettings(this.Context.Site);

    return viewModel;
  }
}