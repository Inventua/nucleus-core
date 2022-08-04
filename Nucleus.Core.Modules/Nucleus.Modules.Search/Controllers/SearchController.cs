using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Search;
using Nucleus.Extensions.Authorization;
using Nucleus.ViewFeatures;

namespace Nucleus.Modules.Search.Controllers
{
	[Extension("Search")]
	public class SearchController : Controller
	{
		private Context Context { get; }
		private IPageManager PageManager { get; }
		private IPageModuleManager PageModuleManager { get; }
		private IUserManager UserManager { get; }
		private IEnumerable<ISearchProvider> SearchProviders { get; }

		private const string MODULESETTING_DISPLAY_MODE = "search:display-mode";
		private const string MODULESETTING_RESULTS_PAGE = "search:results-page";
		private const string MODULESETTING_SEARCH_BUTTON_CAPTION = "search:search-button-caption";
		private const string MODULESETTING_INCLUDE_FILES = "search:include-files";
		private const string MODULESETTING_SHOW_URL = "search:show-url"; 
		private const string MODULESETTING_SHOW_CATEGORIES = "search:show-categories";
		private const string MODULESETTING_SHOW_PUBLISHEDDATE = "search:show-publisheddate";
		private const string MODULESETTING_SHOW_SIZE = "search:show-size";
		private const string MODULESETTING_SHOW_SCORE = "search:show-score";

		public SearchController(Context Context, IPageManager pageManager, IPageModuleManager pageModuleManager, IUserManager userManager, IEnumerable<ISearchProvider> searchProviders)
		{
			this.Context = Context;
			this.PageManager= pageManager;
			this.PageModuleManager = pageModuleManager;
			this.UserManager = userManager;
			this.SearchProviders = searchProviders;			
		}

		[HttpGet]
		public async Task<ActionResult> Index(string search)
		{
			return View("Viewer", await BuildViewModel(new() { SearchTerm = search }));
		}

		[HttpPost]
		public async Task<ActionResult> Index(ViewModels.Viewer viewModel)
		{
			Page resultsPage = null;
			Guid resultsPageId = this.Context.Module.ModuleSettings.Get(MODULESETTING_RESULTS_PAGE, Guid.Empty);
			
			if (resultsPageId != Guid.Empty)
			{
				resultsPage = await this.PageManager.Get(resultsPageId);		
			}
			
			if (resultsPage == null)
			{
				return View("Viewer", await BuildViewModel(viewModel));
			}
			else
			{
				ControllerContext.HttpContext.Response.Headers.Add("X-Location", Url.Content(Url.PageLink(resultsPage) + $"?search={viewModel.SearchTerm}"));
				return StatusCode((int)System.Net.HttpStatusCode.Found);
			}
		}

		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Suggest(ViewModels.Suggestions viewModel)
		{
			return View("_Suggestions", await BuildSuggestViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpGet]
		[HttpPost]
		public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", await BuildSettingsViewModel(viewModel));
		}

		[HttpGet]
		public async Task<ActionResult> GetChildPages(Guid id)
		{
			ViewModels.PageIndexPartial viewModel = new();

			viewModel.FromPage = await this.PageManager.Get(id);

			viewModel.Pages = await this.PageManager.GetAdminMenu
				(
					this.Context.Site,
					await this.PageManager.Get(id),
					ControllerContext.HttpContext.User,
					1
				);

			return View("_PageMenu", viewModel);
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
		{
			this.Context.Module.ModuleSettings.Set(MODULESETTING_RESULTS_PAGE, viewModel.ResultsPageId);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_DISPLAY_MODE, viewModel.DisplayMode);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SEARCH_BUTTON_CAPTION, viewModel.SearchButtonCaption);			
			this.Context.Module.ModuleSettings.Set(MODULESETTING_INCLUDE_FILES, viewModel.IncludeFiles);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_URL, viewModel.ShowUrl); 
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_CATEGORIES, viewModel.ShowCategories);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_PUBLISHEDDATE, viewModel.ShowPublishDate);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_SIZE, viewModel.ShowSize);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_SCORE, viewModel.ShowScore);

			await this.PageModuleManager.SaveSettings(this.Context.Module);

			return Ok();
		}

		private async Task<ViewModels.Viewer> BuildViewModel(ViewModels.Viewer viewModel)
		{
			ISearchProvider searchProvider = null;

			if (viewModel == null)
			{
				viewModel = new();				
			}

			GetSettings(viewModel.Settings);
			
			if (!String.IsNullOrEmpty(viewModel.SearchTerm))
			{
				if (this.SearchProviders.Count() == 1)
				{
					searchProvider = this.SearchProviders.First();
				}

				if (searchProvider == null)
				{
					throw new InvalidOperationException("There is no search provider available.");
				}

				viewModel.Site = this.Context.Site;

				if (!(HttpContext.User.IsSystemAdministrator() | HttpContext.User.IsSiteAdmin(this.Context.Site)))
				{
					viewModel.Roles = (await this.UserManager.Get(this.Context.Site, HttpContext.User.GetUserId()))?.Roles;
				}
				
				if (!viewModel.Settings.IncludeFiles)
				{
					viewModel.ExcludedScopes.Add(Abstractions.Models.FileSystem.File.URN);
				}

				SearchResults results = await searchProvider.Search(viewModel);

				viewModel.PagingSettings.TotalCount = Convert.ToInt32(results.Total);
				viewModel.SearchResults = results;
			}
			
			return viewModel;
		}

		private async Task<ViewModels.Suggestions> BuildSuggestViewModel(ViewModels.Suggestions viewModel)
		{
			ISearchProvider searchProvider = null;
			
			if (viewModel == null)
			{
				viewModel = new();
			}

			if (!String.IsNullOrEmpty(viewModel.SearchTerm))
			{
				if (this.SearchProviders.Count() == 1)
				{
					searchProvider = this.SearchProviders.First();
				}
				
				if (searchProvider == null)
				{
					throw new InvalidOperationException("There is no search provider available.");
				}

				viewModel.Site = this.Context.Site;

				if (!(HttpContext.User.IsSystemAdministrator() | HttpContext.User.IsSiteAdmin(this.Context.Site)))
				{
					viewModel.Roles = (await this.UserManager.Get(this.Context.Site, HttpContext.User.GetUserId()))?.Roles;
				}

				if (!this.Context.Module.ModuleSettings.Get(MODULESETTING_INCLUDE_FILES, true))
				{
					viewModel.ExcludedScopes.Add(Abstractions.Models.FileSystem.File.URN);
				}

				try
				{
					SearchResults results = await searchProvider.Suggest(viewModel);

					viewModel.PagingSettings.TotalCount = Convert.ToInt32(results.Total);
					viewModel.SearchResults = results;
				}
				catch (NotImplementedException)
				{
					viewModel.PagingSettings.TotalCount = 0;
					viewModel.SearchResults = null;
				}				
			}

			return viewModel;
		}

		private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			GetSettings(viewModel);

			viewModel.PageMenu = await this.PageManager.GetAdminMenu(this.Context.Site, null, this.ControllerContext.HttpContext.User, 1);

			return viewModel;
		}

		private void GetSettings(ViewModels.Settings settings)
		{
			settings.ResultsPageId = this.Context.Module.ModuleSettings.Get(MODULESETTING_RESULTS_PAGE, Guid.Empty);
			settings.DisplayMode = this.Context.Module.ModuleSettings.Get(MODULESETTING_DISPLAY_MODE, ViewModels.Settings.DisplayModes.Full);
			settings.SearchButtonCaption = this.Context.Module.ModuleSettings.Get(MODULESETTING_SEARCH_BUTTON_CAPTION, "Search");
			settings.IncludeFiles = this.Context.Module.ModuleSettings.Get(MODULESETTING_INCLUDE_FILES, true);
			settings.ShowUrl = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_URL, false);
			settings.ShowCategories = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_CATEGORIES, true);
			settings.ShowPublishDate = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_PUBLISHEDDATE, true);
			settings.ShowSize = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SIZE, true);
			settings.ShowScore = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SCORE, true);

		}
	}
}