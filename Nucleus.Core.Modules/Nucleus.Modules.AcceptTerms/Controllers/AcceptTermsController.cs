using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.ViewFeatures;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Nucleus.Modules.AcceptTerms.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Modules.AcceptTerms.Controllers
{
  [Extension("AcceptTerms")]
  public class AcceptTermsController : Controller
  {
    private Context Context { get; }
    private IPageModuleManager PageModuleManager { get; }
    private AcceptTermsManager AcceptTermsManager { get; }
    private IContentManager ContentManager { get; }

    public AcceptTermsController(Context Context, IPageModuleManager pageModuleManager, AcceptTermsManager acceptTermsManager, IContentManager contentManager)
    {
      this.Context = Context;
      this.PageModuleManager = pageModuleManager;
      this.AcceptTermsManager = acceptTermsManager;
      this.ContentManager = contentManager;
    }

    [HttpGet]
    public async Task<ActionResult> Index()
    {
      Settings settings = new ViewModels.Viewer();
      settings.GetSettings(this.Context.Module, this.HttpContext.Request.GetUserTimeZone());
      settings.AgreementBody = (await this.ContentManager.List(this.Context.Module)).FirstOrDefault();

      if (User.Identity.IsAuthenticated )
      {
        if (User.IsSystemAdministrator() || User.IsSiteAdmin(this.Context.Site) || User.IsEditing(HttpContext))
        {
          // do nothing, user does not have to accept the terms
          return Content("");
        }
        else
        { 
          UserAcceptedTerms userAcceptedTerms = await this.AcceptTermsManager.Get(this.Context.Module, User);
          if (userAcceptedTerms != null && (!settings.EffectiveDate.HasValue || userAcceptedTerms.DateAccepted >= settings.EffectiveDate))
          {
            // suppress module output, user has accepted terms already
            return NoContent();
          }
        }
      }

      return View("Viewer", settings);
    }

    [HttpPost]
    public async Task<ActionResult> SaveUserAcceptedTerms()
    {
      if (User.Identity.IsAuthenticated)
      {
        await this.AcceptTermsManager.Save(this.Context.Module, User);
      }
      return Ok();
      //return await Index();
    }

    [HttpPost]
    public ActionResult CancelTerms()
    {
      string location = Url.Content("~/");
      ControllerContext.HttpContext.Response.Headers.Add("X-Location", location);
      return StatusCode((int)System.Net.HttpStatusCode.Found);
    }
  }
}