using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using Nucleus.Modules.PageLinks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Modules.PageLinks.Controllers;

[Extension("PageLinks")]
public class PageLinksViewerController : Controller
{
	private Context Context { get; }
  private PageLinksManager PageLinksManager { get; }


  public PageLinksViewerController(Context Context, PageLinksManager pageLinksManager)
  {
    this.Context = Context;
    this.PageLinksManager = pageLinksManager;
  }

  [HttpGet]
	public async Task<ActionResult> Index()
	{
		return View("Viewer", await BuildViewModel());
	}

	private async Task <ViewModels.Viewer> BuildViewModel()
	{
		ViewModels.Viewer viewModel = new();

		viewModel.GetSettings(this.Context.Module);

    // Get a comma-separated string of enabled headers element names
    viewModel.EnabledHeaders = string.Join(',', viewModel.IncludeHeaders
      .Where(header => header.Value)
      .Select(header => header.Key));

    if (viewModel.Mode == Settings.Modes.Manual)
    {
      viewModel.PageLinks = (await this.PageLinksManager.List(this.Context.Module))
        .Where(pageLink => !String.IsNullOrEmpty(pageLink.TargetId));
    }

    return viewModel;
	}
}