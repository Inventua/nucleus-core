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
		private const string MODULESETTING_SEARCH_CAPTION = "search:search-caption";
		private const string MODULESETTING_SEARCH_PROMPT = "search:search-prompt";
		private const string MODULESETTING_SEARCH_BUTTON_CAPTION = "search:search-button-caption";
		private const string MODULESETTING_INCLUDE_FILES = "search:include-files";

		private const string MODULESETTING_SHOW_URL = "search:show-url";
		private const string MODULESETTING_SHOW_SUMMARY = "search:show-summary";
		private const string MODULESETTING_SHOW_CATEGORIES = "search:show-categories";
		private const string MODULESETTING_SHOW_PUBLISHEDDATE = "search:show-publisheddate";

		private const string MODULESETTING_SHOW_SIZE = "search:show-size";
		private const string MODULESETTING_SHOW_SCORE = "search:show-score";
		private const string MODULESETTING_INCLUDE_SCOPES = "search:include-scopes";
		private const string MODULESETTING_MAXIMUM_SUGGESTIONS = "search:maximum-suggestions";

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

		/// <summary>
		/// This method is called when the paging control elements are used.
		/// </summary>
		/// <param name="viewModel"></param>
		/// <returns></returns>
		[HttpPost]
		public async Task<ActionResult> Index(ViewModels.Viewer viewModel)
		{
			return View("Viewer", await BuildViewModel(viewModel));
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
			if (viewModel.MaximumSuggestions > 100) viewModel.MaximumSuggestions = 100;

			this.Context.Module.ModuleSettings.Set(MODULESETTING_RESULTS_PAGE, viewModel.ResultsPageId);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_DISPLAY_MODE, viewModel.DisplayMode);
			
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SEARCH_CAPTION, viewModel.SearchCaption);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SEARCH_PROMPT, viewModel.Prompt);
			
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SEARCH_BUTTON_CAPTION, viewModel.SearchButtonCaption);			
			this.Context.Module.ModuleSettings.Set(MODULESETTING_INCLUDE_FILES, viewModel.IncludeFiles);
			
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_URL, viewModel.ShowUrl); 
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_SUMMARY, viewModel.ShowSummary);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_CATEGORIES, viewModel.ShowCategories);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_PUBLISHEDDATE, viewModel.ShowPublishDate);

			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_SIZE, viewModel.ShowSize);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_SHOW_SCORE, viewModel.ShowScore);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_INCLUDE_SCOPES, viewModel.IncludeScopes);
			this.Context.Module.ModuleSettings.Set(MODULESETTING_MAXIMUM_SUGGESTIONS, viewModel.MaximumSuggestions);
			
			await this.PageModuleManager.SaveSettings(this.Context.Module);

			return Ok();
		}

		private async Task<ViewModels.Viewer> BuildViewModel(ViewModels.Viewer viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();				
			}

			GetSettings(viewModel.Settings);

			Page resultsPage = null;
			Guid resultsPageId = this.Context.Module.ModuleSettings.Get(MODULESETTING_RESULTS_PAGE, Guid.Empty);

			if (resultsPageId != Guid.Empty)
			{
				resultsPage = await this.PageManager.Get(resultsPageId);
			}

			if (resultsPage != null)
			{
				viewModel.ResultsUrl = "~" + resultsPage.DefaultPageRoute().Path;
			}
			else
			{
				viewModel.ResultsUrl = "~" + this.Context.Page.DefaultPageRoute().Path;
			}

			if (!String.IsNullOrEmpty(viewModel.SearchTerm))
			{
				ISearchProvider searchProvider = null;

				if (this.SearchProviders.Count() == 1)
				{
					searchProvider = this.SearchProviders.First();
				}

				if (searchProvider == null)
				{
					throw new InvalidOperationException("There is no search provider available.");
				}

				SearchResults results= await searchProvider.Search(await BuildSearchQuery(viewModel.SearchTerm, viewModel.PagingSettings, viewModel.Settings.IncludeFiles,viewModel.Settings.IncludeScopes));

				viewModel.PagingSettings.TotalCount = Convert.ToInt32(results.Total);
				viewModel.SearchResults = results;
			}
			
			return viewModel;
		}

		private async Task<SearchQuery> BuildSearchQuery(string searchTerm, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings,Boolean includeFiles, string includeScopes)
		{			
			SearchQuery searchQuery = new()
			{
				Site = this.Context.Site,
				SearchTerm = searchTerm,
				PagingSettings = pagingSettings
			};

			if (!(HttpContext.User.IsSystemAdministrator() | HttpContext.User.IsSiteAdmin(this.Context.Site)))
			{
				searchQuery.Roles = (await this.UserManager.Get(this.Context.Site, HttpContext.User.GetUserId()))?.Roles;
			}

			if (!includeFiles)
			{
				searchQuery.ExcludedScopes.Add(Abstractions.Models.FileSystem.File.URN);
			}

			if (!String.IsNullOrEmpty(includeScopes))
			{
				// allow \n delimiter when the included scopes are entered in the settings page and ';' delimiter for when the Html Helper
				// or tag helper is being used.
				searchQuery.IncludedScopes = includeScopes.Split(new char[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
			}

			return searchQuery;
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

				if (viewModel.Settings.MaximumSuggestions > 100) viewModel.Settings.MaximumSuggestions = 100;

				try
				{
					viewModel.SearchResults = await searchProvider.Suggest(await BuildSearchQuery
						(
						  viewModel.SearchTerm, 
							new() 
							{  
								CurrentPageIndex = 1, PageSize = viewModel.Settings.MaximumSuggestions 
							}, 
							viewModel.Settings.IncludeFiles, 
							viewModel.Settings.IncludeScopes
						));
				}
				catch (NotImplementedException)
				{
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
			settings.SearchCaption = this.Context.Module.ModuleSettings.Get(MODULESETTING_SEARCH_CAPTION, "Search Term");
			settings.Prompt = this.Context.Module.ModuleSettings.Get(MODULESETTING_SEARCH_PROMPT, Nucleus.Modules.Search.ViewModels.Settings.PROMPT_DEFAULT); 
			settings.SearchButtonCaption = this.Context.Module.ModuleSettings.Get(MODULESETTING_SEARCH_BUTTON_CAPTION, "Search");
			settings.IncludeFiles = this.Context.Module.ModuleSettings.Get(MODULESETTING_INCLUDE_FILES, true);

			settings.ShowUrl = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_URL, false);
			settings.ShowSummary = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SUMMARY, true);			
			settings.ShowCategories = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_CATEGORIES, true);
			settings.ShowPublishDate = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_PUBLISHEDDATE, true);

			settings.ShowSize = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SIZE, true);
			settings.ShowScore = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SCORE, true);
			settings.IncludeScopes = this.Context.Module.ModuleSettings.Get(MODULESETTING_INCLUDE_SCOPES, "");
			settings.MaximumSuggestions = this.Context.Module.ModuleSettings.Get(MODULESETTING_MAXIMUM_SUGGESTIONS, 5);

			if (String.IsNullOrWhiteSpace(settings.SearchButtonCaption))
			{
				settings.SearchButtonCaption = "Search";
			}
		}
	}
}