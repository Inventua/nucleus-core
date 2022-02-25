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

namespace Nucleus.Modules.Search.Controllers
{
	[Extension("Search")]
	public class SearchController : Controller
	{
		private Context Context { get; }
		private IPageModuleManager PageModuleManager { get; }
		private IUserManager UserManager { get; }
		private IEnumerable<ISearchProvider> SearchProviders { get; }

		private const string MODULESETTING_SHOW_URL = "multicontent:show-url"; 
		private const string MODULESETTING_SHOW_CATEGORIES = "multicontent:show-categories";
		private const string MODULESETTING_SHOW_PUBLISHEDDATE = "search:show-publisheddate";
		private const string MODULESETTING_SHOW_SIZE = "search:show-size";
		private const string MODULESETTING_SHOW_SCORE = "search:show-score";

		public SearchController(Context Context, IPageModuleManager pageModuleManager, IUserManager userManager, IEnumerable<ISearchProvider> searchProviders)
		{
			this.Context = Context;
			this.PageModuleManager = pageModuleManager;
			this.UserManager = userManager;
			this.SearchProviders = searchProviders;			
		}

		[HttpGet]
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
		public ActionResult Settings(ViewModels.Settings viewModel)
		{
			return View("Settings", BuildSettingsViewModel(viewModel));
		}

		[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
		[HttpPost]
		public async Task<ActionResult> SaveSettings(ViewModels.Settings viewModel)
		{
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

			viewModel.Settings.ShowUrl = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_URL, false);
			viewModel.Settings.ShowCategories = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_CATEGORIES, true);
			viewModel.Settings.ShowPublishDate = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_PUBLISHEDDATE, true);
			viewModel.Settings.ShowSize = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SIZE, true);
			viewModel.Settings.ShowScore = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SCORE, true);

			if (!String.IsNullOrEmpty(viewModel.SearchTerm))
			{
				if (this.SearchProviders.Count() == 1)
				{
					searchProvider = this.SearchProviders.First();
				}
				//else if ()

				if (searchProvider == null)
				{
					throw new InvalidOperationException("There is no search provider available.");
				}

				viewModel.Site = this.Context.Site;

				if (!(HttpContext.User.IsSystemAdministrator() | HttpContext.User.IsSiteAdmin(this.Context.Site)))
				{
					viewModel.Roles = (await this.UserManager.Get(this.Context.Site, HttpContext.User.GetUserId()))?.Roles;
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

		private ViewModels.Settings BuildSettingsViewModel(ViewModels.Settings viewModel)
		{
			if (viewModel == null)
			{
				viewModel = new();
			}

			viewModel.ShowUrl = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_URL, false);
			viewModel.ShowCategories = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_CATEGORIES, true);
			viewModel.ShowPublishDate = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_PUBLISHEDDATE, true);
			viewModel.ShowSize = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SIZE, true);
			viewModel.ShowScore = this.Context.Module.ModuleSettings.Get(MODULESETTING_SHOW_SCORE, true);

			return viewModel;
		}

	}
}