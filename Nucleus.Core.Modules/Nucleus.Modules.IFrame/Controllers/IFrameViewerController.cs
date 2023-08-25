using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Nucleus.Modules.IFrame.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.IFrame.Controllers;
[Extension("IFrame")]
public class IFrameViewerController : Controller
{
  private Context Context { get; }
  private IPageModuleManager PageModuleManager { get; }

  public IFrameViewerController(Context Context, IPageModuleManager pageModuleManager)
  {
    this.Context = Context;
    this.PageModuleManager = pageModuleManager;
  }

  [HttpGet]
  public ActionResult Index()
  {
    ViewModels.Viewer viewModel = BuildViewModel();

    if (String.IsNullOrEmpty(viewModel.Url))
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
    else
    {
      StringBuilder cssStyle = new();

      // Scrolling
      if (viewModel.Scrolling != Settings.ScrollingOptions.Auto)
      {
        viewModel.FrameAttributes.Add("scrolling", Enum.GetName(typeof(Settings.ScrollingOptions), viewModel.Scrolling));
      }

      cssStyle.Add("width", string.IsNullOrEmpty(viewModel.Width) ? "100%" : viewModel.Width);

      if (!String.IsNullOrEmpty(viewModel.Height))
      {
        cssStyle.Add("height", viewModel.Height);
      }
      else
      {
        cssStyle.Add("flex", "1");
      }

      viewModel.FrameAttributes.Add("frameborder", "0");
      if (viewModel.Border) 
      { 
        cssStyle.Add("border", "1px solid");
      }

      if (!string.IsNullOrEmpty(viewModel.Name))
      {
        viewModel.FrameAttributes.Add("name", viewModel.Name);
      }

      if (!string.IsNullOrEmpty(viewModel.Title))
      {
        viewModel.FrameAttributes.Add("title", viewModel.Title);
      }

      if (cssStyle.Length > 0)
      {
        viewModel.FrameAttributes.Add("style", cssStyle.ToString());
      }
      
      return View("Viewer", viewModel);
    }
  }

  private ViewModels.Viewer BuildViewModel()
  {
    ViewModels.Viewer viewModel = new();

    viewModel.GetSettings(this.Context.Module);
    
    return viewModel;
  }
}