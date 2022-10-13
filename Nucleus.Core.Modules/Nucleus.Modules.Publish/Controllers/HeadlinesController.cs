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
    private string ViewerLayoutPath { get; }

    public HeadlinesController(Context Context, ArticlesManager articlesManager, IPageManager pageManager, IPageModuleManager pageModuleManager, HeadlinesManager headlinesManager)
    {
      this.Context = Context;
      this.ArticlesManager = articlesManager;
      this.PageManager = pageManager;
      this.PageModuleManager = pageModuleManager;
      this.HeadlinesManager = headlinesManager;
      
      this.ViewerLayoutPath = $"~/{Nucleus.Abstractions.Models.Configuration.FolderOptions.EXTENSIONS_FOLDER}/Publish/Views/ViewerLayouts";
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
      viewModel.HeadlineSettings.GetSettings(this.Context.Module);

      if (viewModel.HeadlineSettings.LinkedModuleId != Guid.Empty)
      {
        Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings = viewModel.Articles == null ? new() : viewModel.Articles;

        PageModule linkedModule = await this.PageModuleManager.Get(viewModel.HeadlineSettings.LinkedModuleId);
                
        viewModel.Articles =  await this.ArticlesManager.List(this.Context.Site, linkedModule, pagingSettings, await this.HeadlinesManager.GetFilterOptions(this.Context.Module));

        viewModel.PrimaryArticles = Enumerable.Empty<Article>();
        viewModel.SecondaryArticles = Enumerable.Empty<Article>();

        if (viewModel.Articles.CurrentPageIndex == 1)
        {
          if (!String.IsNullOrEmpty(viewModel.HeadlineSettings.LayoutOptions.PrimaryArticleLayout))
          {
            // primary layout/secondary layout applies to first page only.
            viewModel.PrimaryArticles = viewModel.Articles.Items
              .Where(article => IsPrimary(article, viewModel.HeadlineSettings.LayoutOptions.PrimaryFeaturedOnly))
              .TakeArticles(viewModel.HeadlineSettings.LayoutOptions.PrimaryArticleCount);
          }

          if (!String.IsNullOrEmpty(viewModel.HeadlineSettings.LayoutOptions.SecondaryArticleLayout))
          {
            viewModel.SecondaryArticles = viewModel.Articles.Items
              .Skip(viewModel.PrimaryArticles.Count())
              .TakeArticles(viewModel.HeadlineSettings.LayoutOptions.SecondaryArticleCount);
          }
        }
        else
        {
          // subsequent pages: all articles use the same article layout.  Default is the secondary layout unless no secondary layout is selected.
          if (String.IsNullOrEmpty(viewModel.HeadlineSettings.LayoutOptions.SecondaryArticleLayout))
          {
            // secondary layout selection is not mandatory: if not selected then all articles are shown with primary layout.
            viewModel.PrimaryArticles = viewModel.Articles.Items;
          }
          else
          {
            // secondary layout selection is selected, use secondary layout for all articles.
            viewModel.SecondaryArticles = viewModel.Articles.Items;
          }
        }

        viewModel.Context = new() {Site = this.Context.Site, Page = await this.PageManager.Get(linkedModule.PageId), Module = linkedModule };

      }

      viewModel.MasterLayoutPath = viewModel.HeadlineSettings.LayoutOptions.GetMasterLayoutPath(this.ViewerLayoutPath);
      viewModel.PrimaryArticleLayoutPath = viewModel.HeadlineSettings.LayoutOptions.GetPrimaryArticleLayoutPath(this.ViewerLayoutPath);
      viewModel.SecondaryArticleLayoutPath = viewModel.HeadlineSettings.LayoutOptions.GetSecondaryArticleLayoutPath(this.ViewerLayoutPath);

      return viewModel;
    }

    private Boolean IsPrimary(Article article, Boolean featuredOnly)
    {
      if (featuredOnly)
      {
        return article.Featured;
      }
      else
      {
        return true;
      }
    }
  }
}
