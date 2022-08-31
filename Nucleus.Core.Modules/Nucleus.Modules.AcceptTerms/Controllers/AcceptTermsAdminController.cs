using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.Modules.AcceptTerms.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Modules.AcceptTerms.Controllers
{
  [Extension("AcceptTerms")]
  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  public class AcceptTermsAdminController : Controller
  {
    private Context Context { get; }
    private IPageModuleManager PageModuleManager { get; }
    private AcceptTermsManager AcceptTermsManager { get; }
    private IContentManager ContentManager { get; }

    public AcceptTermsAdminController(Context Context, IPageModuleManager pageModuleManager, AcceptTermsManager acceptTermsManager, IContentManager contentManager)
    {
      this.Context = Context;
      this.PageModuleManager = pageModuleManager;
      this.AcceptTermsManager = acceptTermsManager;
      this.ContentManager = contentManager;
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
      //DateTime? effectiveDateUtc = (viewModel.EffectiveDate.HasValue ? System.TimeZoneInfo.ConvertTimeToUtc(viewModel.EffectiveDate.Value, this.HttpContext.Request.GetUserTimeZone()) : (DateTime?)null);

      //this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_TITLE, viewModel.Title);
      //this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_ACCEPTTEXT, viewModel.AcceptText);
      //this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_CANCELTEXT, viewModel.CancelText);
      //this.Context.Module.ModuleSettings.Set(Models.Settings.MODULESETTING_EFFECTIVEDATE, effectiveDateUtc);

      viewModel.SetSettings(this.Context.Module, this.HttpContext.Request.GetUserTimeZone());
      await this.PageModuleManager.SaveSettings(this.Context.Module);

      await this.ContentManager.Save(this.Context.Module, viewModel.AgreementBody);

      return Ok();
    }

    private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
    {
      if (viewModel == null)
      {
        viewModel = new();
      }

      viewModel.GetSettings(this.Context.Module, this.HttpContext.Request.GetUserTimeZone());
      viewModel.AgreementBody = (await this.ContentManager.List(this.Context.Module)).FirstOrDefault();

      return viewModel;
    }

  }
}