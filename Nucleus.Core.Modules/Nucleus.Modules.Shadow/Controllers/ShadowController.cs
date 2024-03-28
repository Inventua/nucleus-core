using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Layout;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions.Shadow.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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
  public ActionResult Index()
  {
    ViewModels.Viewer viewModel = BuildViewModel();

    if (viewModel.ModuleId != Guid.Empty)
    {
      // module is configured.  
      // return a HttpStatusCode.PermanentRedirect to signal the ModuleContentRenderer to render a different 
      // module.  The ModuleContentRenderer also requires the X-NucleusAction header, in order to ensure that it is not 
      // intercepting a "real" HttpStatusCode.PermanentRedirect response.
      HttpContext.Response.Headers["X-NucleusAction"] = "";
      
      return new Microsoft.AspNetCore.Mvc.LocalRedirectResult($"~/mid={viewModel.ModuleId}", true, true);
    }
    else
    {
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
  }

  private ViewModels.Viewer BuildViewModel()
  {
    ViewModels.Viewer viewModel = new();

    viewModel.GetSettings(this.Context.Module);

    return viewModel;
  }
}