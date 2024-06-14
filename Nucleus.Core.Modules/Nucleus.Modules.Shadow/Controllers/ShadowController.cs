using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Extensions.Shadow.Controllers;

[Extension("Shadow")]
public class ShadowController : Controller
{
  private Context Context { get; }
  private IPageModuleManager PageModuleManager { get; }

  public ShadowController(Context Context, IPageModuleManager pageModuleManager)
  {
    this.Context = Context;
    this.PageModuleManager = pageModuleManager;
  }

  [HttpGet]
  public async Task<ActionResult> Index()
  {
    ViewModels.Viewer viewModel = BuildViewModel();

    if (viewModel.ModuleId != Guid.Empty)
    {
      // module is configured.  

      // check that module exists
      PageModule module = await this.PageModuleManager.Get(viewModel.ModuleId);

      if (module != null)
      {
        // return a HttpStatusCode.PermanentRedirect to signal the ModuleContentRenderer to render a different 
        // module.  The ModuleContentRenderer also requires the X-NucleusAction header, in order to ensure that it is not 
        // intercepting a "real" HttpStatusCode.PermanentRedirect response. 
        HttpContext.Response.Headers["X-NucleusAction"] = "Shadow";
        return new Microsoft.AspNetCore.Mvc.LocalRedirectResult($"/mid={viewModel.ModuleId}", true, true);
      }
    }

    // unconfigured module, or redirect to module does not exist
    if (User.CanEditContent(this.Context.Site, this.Context.Page))
    {
      // un-configured module, admin user, display a warning
      return View("Viewer", BuildViewModel());
    }
    else
    {
      // un-configured module, regular user, return NoContent to prevent rendering of the module
      return new NoContentResult();
    }
  }

  private ViewModels.Viewer BuildViewModel()
  {
    ViewModels.Viewer viewModel = new();

    viewModel.GetSettings(this.Context.Module);

    return viewModel;
  }
}