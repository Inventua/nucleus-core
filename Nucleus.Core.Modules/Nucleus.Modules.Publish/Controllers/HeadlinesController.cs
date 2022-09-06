using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Extensions;
using Nucleus.Modules.Publish.Models;

namespace Nucleus.Modules.Publish.Controllers
{
  [Extension("Publish")]
  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  public class HeadlinesController : Controller
  {

    private Context Context { get; }
    private ArticlesManager ArticlesManager { get; }
    private IPageManager PageManager { get; }
    private IPageModuleManager PageModuleManager { get; } 
    private HeadlinesManager HeadlinesManager { get; }

    public HeadlinesController(Context Context, ArticlesManager articlesManager, IPageManager pageManager, IPageModuleManager pageModuleManager, HeadlinesManager headlinesManager)
    {
      this.Context = Context;
      this.ArticlesManager = articlesManager;
      this.PageManager = pageManager;
      this.PageModuleManager = pageModuleManager;
      this.HeadlinesManager = headlinesManager;
    }

    [HttpGet]
    [HttpPost]
    public async Task<ActionResult> Index(ViewModels.HeadlinesViewer viewModel)
    {
      return View("HeadlinesViewer", await BuildViewerViewModel());
    }

    private async Task<ViewModels.HeadlinesViewer> BuildViewerViewModel()
    {
      return await BuildViewerViewModel(null);
    }

    private async Task<ViewModels.HeadlinesViewer> BuildViewerViewModel(ViewModels.HeadlinesViewer viewModel)
    {

      if (viewModel == null)
      {
        viewModel = new();
      }

      viewModel.Settings.GetSettings(this.Context.Module);

      if (viewModel.Settings.LinkedModuleId != Guid.Empty)
      {
        Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings = viewModel.Articles == null ? new() : viewModel.Articles;

        PageModule linkedModule = await this.PageModuleManager.Get(viewModel.Settings.LinkedModuleId);

        viewModel.Articles = await this.ArticlesManager.List(this.Context.Site, linkedModule, pagingSettings, await this.HeadlinesManager.GetFilterOptions(this.Context.Module));
        viewModel.Context = new() {Site = this.Context.Site, Page = await this.PageManager.Get(linkedModule.PageId), Module = linkedModule } ;
      }
      
      viewModel.Layout = $"ViewerLayouts/{viewModel.Settings.Layout}.cshtml";

      return viewModel;

    }
  }
}
