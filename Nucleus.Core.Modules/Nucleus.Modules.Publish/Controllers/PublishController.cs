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

namespace Nucleus.Modules.Publish.Controllers
{
	[Extension("Publish")]
	public class PublishController : Controller
	{
		private Context Context { get; }
		private ArticlesManager ArticlesManager { get; }
		private IListManager ListManager { get; }
		private IWebHostEnvironment WebHostEnvironment { get; }


		private const string MODULESETTING_CATEGORYLIST_ID = "articles:categorylistid";
		private const string MODULESETTING_LAYOUT = "articles:layout";

		public PublishController(IWebHostEnvironment webHostEnvironment, Context Context, ArticlesManager articlesManager, IListManager listManager)
		{
			this.WebHostEnvironment = webHostEnvironment;
			this.Context = Context;
			this.ArticlesManager = articlesManager;
			this.ListManager = listManager;
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

			ModelState.Clear();

			viewModel.Context = this.Context;
			viewModel.ModuleId = this.Context.Module.Id;
			viewModel.Layout = $"ViewerLayouts/{this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table")}.cshtml";
			viewModel.Articles = await this.ArticlesManager.List(this.Context.Site, this.Context.Module, settings);
			
			return viewModel;
		}

		private Task<ViewModels.ViewArticle> BuildViewerViewModel(Models.Article article)
		{
			ViewModels.ViewArticle viewModel = new() 
			{
				Article = article
			};
			return Task.FromResult(viewModel);
		}

	}
}