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
	private IPageModuleManager PageModuleManager { get; }

	public PageLinksViewerController(Context Context, IPageModuleManager pageModuleManager)
	{
		this.Context = Context;
		this.PageModuleManager = pageModuleManager;
	}

	[HttpGet]
	public ActionResult Index()
	{
		return View("Viewer", BuildViewModel());
	}

	private ViewModels.Viewer BuildViewModel()
	{
		ViewModels.Viewer viewModel = new();

		viewModel.GetSettings(this.Context.Module);
		return viewModel;
	}
}