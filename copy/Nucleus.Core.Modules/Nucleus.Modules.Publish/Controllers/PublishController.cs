using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Paging;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Core;
using Nucleus.Core.Authorization;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.FileSystemProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.Modules.Publish.Controllers
{
	[Extension("Publish")]
	public class PublishController : Controller
	{
		private Context Context { get; }
		private ArticlesManager ArticlesManager { get; }
		private ListManager ListManager { get; }
		private IWebHostEnvironment WebHostEnvironment { get; }


		private const string MODULESETTING_CATEGORYLIST_ID = "articles:categorylistid";
		private const string MODULESETTING_LAYOUT = "articles:layout";

		public PublishController(IWebHostEnvironment webHostEnvironment, Context Context, ArticlesManager articlesManager, ListManager listManager)
		{
			this.WebHostEnvironment = webHostEnvironment;
			this.Context = Context;
			this.ArticlesManager = articlesManager;
			this.ListManager = listManager;
		}

		[HttpGet]
		[HttpPost]
		public ActionResult Index(ViewModels.Viewer viewModel)
		{
			if (String.IsNullOrEmpty(this.Context.Parameters))
			{
				return View("Viewer", BuildViewerViewModel(viewModel));
			}
			else
			{
				// display selected forum
				Models.Article article = this.ArticlesManager.Find(this.Context.Site, this.Context.Module, this.Context.Parameters);
				if (article != null)
				{
					return View("ViewArticle", BuildViewerViewModel(article));
				}
				else
				{
					return NotFound();
				}
			}			
		}

		[Authorize(Policy = Nucleus.Core.Authorization.ModuleEditPermissionAuthorizationHandler.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public ActionResult Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		private ViewModels.Viewer BuildViewerViewModel (ViewModels.Viewer viewModel)
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

			viewModel.ModuleId = this.Context.Module.Id;
			viewModel.Layout = $"ViewerLayouts/{this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table")}.cshtml";
			viewModel.Articles = this.ArticlesManager.List(this.Context.Site, this.Context.Module, settings);
			
			return viewModel;
		}

		private ViewModels.ViewArticle BuildViewerViewModel(Models.Article article)
		{
			ViewModels.ViewArticle viewModel = new() 
			{
				Article = article
			};
			return viewModel;
		}

		private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			{
				if (viewModel == null)
				{
					viewModel = new();
				}

				viewModel.ModuleId = this.Context.Module.Id;
				viewModel.Articles = this.ArticlesManager.List(this.Context.Module);
				viewModel.Lists = this.ListManager.List(this.Context.Site);
				viewModel.CategoryList = this.ListManager.Get(this.Context.Module.ModuleSettings.Get(MODULESETTING_CATEGORYLIST_ID, Guid.Empty));
				viewModel.Layout = this.Context.Module.ModuleSettings.Get(MODULESETTING_LAYOUT, "Table");

				viewModel.Layouts = new();
				foreach (string file in System.IO.Directory.EnumerateFiles($"{this.WebHostEnvironment.ContentRootPath}\\{Constants.EXTENSIONS_ROUTE_PATH}\\Publish\\Views\\ViewerLayouts\\", "*.cshtml").OrderBy(layout=>layout))
				{
					viewModel.Layouts.Add(System.IO.Path.GetFileNameWithoutExtension(file));
				}

				return viewModel;
			}
		}
	}
}