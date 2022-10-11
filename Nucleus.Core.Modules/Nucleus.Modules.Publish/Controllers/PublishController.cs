using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Paging;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using Nucleus.Extensions;
using System.Threading.Tasks;
using Nucleus.Modules.Publish.Models;

namespace Nucleus.Modules.Publish.Controllers
{
	[Extension("Publish")]
	public class PublishController : Controller
	{
		private Context Context { get; }
		private ArticlesManager ArticlesManager { get; }
		private IListManager ListManager { get; }
		private IWebHostEnvironment WebHostEnvironment { get; }
    private string ViewerLayoutPath { get; }

    private const string MODULESETTING_CATEGORYLIST_ID = "articles:categorylistid";
		private const string MODULESETTING_LAYOUT = "articles:layout";

		public PublishController(IWebHostEnvironment webHostEnvironment, Context Context, ArticlesManager articlesManager, IListManager listManager)
		{
			this.WebHostEnvironment = webHostEnvironment;
			this.Context = Context;
			this.ArticlesManager = articlesManager;
			this.ListManager = listManager;
      this.ViewerLayoutPath = $"~/{Nucleus.Abstractions.Models.Configuration.FolderOptions.EXTENSIONS_FOLDER}/Publish/Views/ViewerLayouts";
    }

    [HttpGet]
		[HttpPost]
		public async Task<ActionResult> Index(ViewModels.Viewer viewModel)
		{
			if (!this.Context.LocalPath.HasValue)
			{
				return View("Viewer", await BuildViewerViewModel(viewModel));
			}
			else
			{
				// display selected article
				Models.Article article = await this.ArticlesManager.Find(this.Context.Site, this.Context.Module, this.Context.LocalPath.FullPath);
				if (article != null)
				{
					return View("ViewArticle", await BuildViewerViewModel(article));
				}
				else
				{
					return NotFound();
				}
			}			
		}

		private async Task<ViewModels.Viewer> BuildViewerViewModel (ViewModels.Viewer viewModel)
		{
			PagingSettings settings;
			if (viewModel.Articles == null)
			{
				settings = new();
			}
			else
			{
				settings = viewModel.Articles;
			}
      viewModel.Settings.GetSettings(this.Context.Module);

      ModelState.Clear();

			viewModel.Context = this.Context;
			viewModel.ModuleId = this.Context.Module.Id;

      viewModel.MasterLayoutPath = viewModel.Settings.LayoutOptions.GetMasterLayoutPath(this.ViewerLayoutPath);
      viewModel.PrimaryArticleLayoutPath = viewModel.Settings.LayoutOptions.GetPrimaryArticleLayoutPath(this.ViewerLayoutPath);
      viewModel.SecondaryArticleLayoutPath = viewModel.Settings.LayoutOptions.GetSecondaryArticleLayoutPath(this.ViewerLayoutPath);

      viewModel.Articles = await this.ArticlesManager.List(this.Context.Site, this.Context.Module, settings);
      viewModel.PrimaryArticles = Enumerable.Empty<Article>();
      viewModel.SecondaryArticles = Enumerable.Empty<Article>();

      if (viewModel.Articles.CurrentPageIndex == 1)
      {
        if (!String.IsNullOrEmpty(viewModel.Settings.LayoutOptions.PrimaryArticleLayout))
        {
          // primary layout/secondary layout applies to first page only.
          viewModel.PrimaryArticles = viewModel.Articles.Items
            .Where(article => IsPrimary(article, viewModel.Settings.LayoutOptions.PrimaryFeaturedOnly))
            .TakeArticles(viewModel.Settings.LayoutOptions.PrimaryArticleCount);
        }

        if (!String.IsNullOrEmpty(viewModel.Settings.LayoutOptions.SecondaryArticleLayout))
        {
          viewModel.SecondaryArticles = viewModel.Articles.Items
            .Skip(viewModel.PrimaryArticles.Count())
            .TakeArticles(viewModel.Settings.LayoutOptions.SecondaryArticleCount);
        }
      }
      else
      {
        // subsequent pages: all articles use the same article layout.  Default is the secondary layout unless no secondary layout is selected.
        if (String.IsNullOrEmpty(viewModel.Settings.LayoutOptions.SecondaryArticleLayout))
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

			return viewModel;
		}

		private Task<ViewModels.ViewArticle> BuildViewerViewModel(Models.Article article)
		{
			ViewModels.ViewArticle viewModel = new() 
			{
				Context = this.Context,
				Article = article
			};
			return Task.FromResult(viewModel);
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