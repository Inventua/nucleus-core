using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Search;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;
using Nucleus.Extensions.Logging;

namespace Nucleus.Modules.Search.Controllers;

[Extension("Search")]
public class SearchController : Controller
{
  private Context Context { get; }
  private IPageManager PageManager { get; }
  private IPageModuleManager PageModuleManager { get; }
  private IUserManager UserManager { get; }
  private IEnumerable<ISearchProvider> SearchProviders { get; }
  private ILogger<SearchController> Logger { get; }

  public SearchController(Context Context, IPageManager pageManager, IPageModuleManager pageModuleManager, IUserManager userManager, IEnumerable<ISearchProvider> searchProviders, ILogger<SearchController> logger)
  {
    this.Context = Context;
    this.PageManager = pageManager;
    this.PageModuleManager = pageModuleManager;
    this.UserManager = userManager;
    this.SearchProviders = searchProviders;
    this.Logger = logger;
  }

  [HttpGet]
  public async Task<ActionResult> Index(string search)
  {
    ModelState.Clear();
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
    ModelState.Clear();
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
  public async Task<ActionResult> Settings()
  {
    return View("Settings", await BuildSettingsViewModel(null));
  }

  [Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.MODULE_EDIT_POLICY)]
  [HttpPost]
  public async Task<ActionResult> Settings(ViewModels.Settings viewModel)
  {
    ModelState.Clear();
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

    viewModel.SetSettings(this.Context.Module);

    await this.PageModuleManager.SaveSettings(this.Context.Page, this.Context.Module);

    return Ok();
  }

  private async Task<ViewModels.Viewer> BuildViewModel(ViewModels.Viewer viewModel)
  {
    if (viewModel == null)
    {
      viewModel = new();
    }

    viewModel.ModuleId = this.Context.Module.Id;
    viewModel.GetSettings(this.Context.Module);

    Page resultsPage = null;
    Guid resultsPageId = viewModel.ResultsPageId;  //this.Context.Module.ModuleSettings.Get(MODULESETTING_RESULTS_PAGE, Guid.Empty);

    if (resultsPageId != Guid.Empty)
    {
      resultsPage = await this.PageManager.Get(resultsPageId);
    }
    
    viewModel.ResultsUrl = resultsPage==null ? "" : "~" + resultsPage.DefaultPageRoute().Path;
    
    ISearchProvider searchProvider = GetSelectedSearchProvider(viewModel.SearchProvider); //.Settings);

    GetSearchCapabilities(viewModel, searchProvider);

    if (!String.IsNullOrEmpty(viewModel.SearchTerm))
    {
      if (searchProvider == null)
      {
        throw new InvalidOperationException("There is no search provider selected.");
      }

      SearchResults results = await searchProvider.Search(await BuildSearchQuery(viewModel));

      viewModel.PagingSettings.TotalCount = Convert.ToInt32(results.Total);
      viewModel.SearchResults = results;
    }

    return viewModel;
  }

  private async Task<SearchQuery> BuildSearchQuery(ViewModels.Suggestions viewModel)
  {
    SearchQuery searchQuery = new()
    {
      Site = this.Context.Site,
      SearchTerm = viewModel.SearchTerm,
      StrictSearchTerms = viewModel.SearchMode == ViewModels.Settings.SearchModes.All,
      PagingSettings = new()
      {
        CurrentPageIndex = 1,
        PageSize = viewModel.MaximumSuggestions
      }
    };

    List<Role> roles = new() { this.Context.Site.AllUsersRole };

    if (HttpContext.User.IsSiteAdmin(this.Context.Site))
    {
      roles = null;  // roles=null means don't filter results by role
    }
    else if (HttpContext.User.IsAnonymous())
    {
      roles.Add(this.Context.Site.AnonymousUsersRole);
    }
    else
    {
      roles.AddRange((await this.UserManager.Get(this.Context.Site, HttpContext.User.GetUserId()))?.Roles);
    }

    searchQuery.Roles = roles;

    if (!viewModel.IncludeFiles)
    {
      searchQuery.ExcludedScopes.Add(Abstractions.Models.FileSystem.File.URN);
    }

    // folders are never included in a standard search
    searchQuery.ExcludedScopes.Add(Abstractions.Models.FileSystem.Folder.URN);

    if (!String.IsNullOrEmpty(viewModel.IncludeScopes))
    {
      // allow \n delimiter when the included scopes are entered in the settings page and ';' delimiter for when the Html Helper
      // or tag helper is being used.
      searchQuery.IncludedScopes = viewModel.IncludeScopes.Split(new char[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    return searchQuery;
  }

  private async Task<SearchQuery> BuildSearchQuery(ViewModels.Viewer viewModel)
  {
    SearchQuery searchQuery = new()
    {
      Site = this.Context.Site,
      SearchTerm = viewModel.SearchTerm,
      StrictSearchTerms = viewModel.SearchMode == ViewModels.Settings.SearchModes.All,
      PagingSettings = viewModel.PagingSettings
    };

    List<Role> roles = new() { this.Context.Site.AllUsersRole };

    if (HttpContext.User.IsSiteAdmin(this.Context.Site))
    {
      roles = null;  // roles=null means don't filter results by role
    }
    else if (HttpContext.User.IsAnonymous())
    {
      roles.Add(this.Context.Site.AnonymousUsersRole);
    }
    else
    {
      roles.AddRange((await this.UserManager.Get(this.Context.Site, HttpContext.User.GetUserId()))?.Roles);
    }

    searchQuery.Roles = roles;

    if (!viewModel.IncludeFiles)
    {
      searchQuery.ExcludedScopes.Add(Abstractions.Models.FileSystem.File.URN);
    }

    // folders are never included in a standard search
    searchQuery.ExcludedScopes.Add(Abstractions.Models.FileSystem.Folder.URN);

    if (!String.IsNullOrEmpty(viewModel.IncludeScopes))
    {
      // allow \n delimiter when the included scopes are entered in the settings page and ';' delimiter for when the Html Helper
      // or tag helper is being used.
      searchQuery.IncludedScopes = viewModel.IncludeScopes.Split(new char[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
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

    if (this.Context.Module != null)
    {
      viewModel.GetSettings(this.Context.Module);
    }

    if (!String.IsNullOrEmpty(viewModel.SearchTerm))
    {
      if (this.SearchProviders.Any())
      {
        if (this.SearchProviders.Count() == 1)
        {
          searchProvider = this.SearchProviders.First();
        }
        else
        {
          searchProvider = this.SearchProviders.Where(provider => provider.GetType().FullName.Equals(viewModel.SearchProvider)).FirstOrDefault();
        }
      }

      if (searchProvider == null)
      {
        // don't show an error if there's no search provider selected, search suggestions aren't critical
        //throw new InvalidOperationException("There is no search provider selected.");
        viewModel.SearchResults = new() { Total = 0 };
      }
      else
      {
        if (viewModel.MaximumSuggestions > 100) viewModel.MaximumSuggestions = 100;

        if (viewModel.MaximumSuggestions == 0)
        {
          viewModel.SearchResults = null;
        }
        else
        {
          try
          {
            viewModel.SearchResults = await searchProvider.Suggest(await BuildSearchQuery(viewModel));
          }
          catch (InvalidOperationException ex)
          {
            this.Logger?.LogWarning(ex.Message);

            if (User.IsSiteAdmin(this.Context.Site))
            {
              viewModel.SearchResults = new()
              {
                Total = 1,
                Results = new List<SearchResult>() { new() { Title = ex.Message } }
              };
            }
            else
            {
              viewModel.SearchResults = null;
            }
          }
          catch (NotImplementedException)
          {
            viewModel.SearchResults = null;
          }
        }
      }
    }

    return viewModel;
  }

  private async Task<ViewModels.Settings> BuildSettingsViewModel(ViewModels.Settings viewModel)
  {
    if (viewModel == null)
    {
      viewModel = new();
      viewModel.GetSettings(this.Context.Module);
    }

    viewModel.PageMenu = await this.PageManager.GetAdminMenu(this.Context.Site, null, this.ControllerContext.HttpContext.User, 1);
    viewModel.SearchProviders = this.SearchProviders
      .Select(provider => new ViewModels.Settings.AvailableSearchProvider() { Name = GetFriendlyName(provider.GetType()), ClassName = provider.GetType().FullName })
      .OrderBy(provider => provider.Name)
      .ToList();

    viewModel.SearchProviderCapabilities = GetSelectedSearchProvider(viewModel.SearchProvider)?.GetCapabilities() ?? new Abstractions.Search.DefaultSearchProviderCapabilities();

    return viewModel;
  }

  private ISearchProvider GetSelectedSearchProvider(string selectedSearchProvider) //ViewModels.Viewer viewModel)
  {
    ISearchProvider searchProvider = null;
    if (!String.IsNullOrEmpty(selectedSearchProvider))
    {
      // use selected
      searchProvider = this.SearchProviders
        .Where(provider => provider.GetType().FullName.Equals(selectedSearchProvider))
        .FirstOrDefault();
    }
    else
    {
      // use site default
      if (this.Context.Site.SiteSettings.TryGetValue(Site.SiteSearchSettingsKeys.DEFAULT_SEARCH_PROVIDER, out string defaultSearchProvider))
      {
        searchProvider = this.SearchProviders
          .Where(provider => provider.GetType().FullName == defaultSearchProvider).FirstOrDefault();
      }
    }

    if (searchProvider == null && this.SearchProviders.Count() == 1)
    {
      // selected/site default is not set, use first provider, if there is only one
      searchProvider = this.SearchProviders.First();
    }

    return searchProvider;
  }

  private void GetSearchCapabilities(ViewModels.Viewer viewModel, ISearchProvider searchProvider)
  {
    Abstractions.Search.ISearchProviderCapabilities searchProviderCapabilities;
    searchProviderCapabilities = searchProvider?.GetCapabilities() ?? new Abstractions.Search.DefaultSearchProviderCapabilities();

    // override unsupported settings
    if (searchProviderCapabilities.MaximumSuggestions < viewModel.MaximumSuggestions)
    {
      viewModel.MaximumSuggestions = searchProviderCapabilities.MaximumSuggestions;
    }

    foreach (int pageSize in viewModel.PagingSettings.PageSizes.ToList())
    {
      if (searchProviderCapabilities.MaximumPageSize < pageSize)
      {
        viewModel.PagingSettings.PageSizes.Remove(pageSize);
      }
    }
    if (!searchProviderCapabilities.CanReportPublishedDate)
    {
      viewModel.ShowPublishDate = false;
    }

    if (!searchProviderCapabilities.CanReportCategories)
    {
      viewModel.ShowCategories = false;
    }

    if (!searchProviderCapabilities.CanReportType)
    {
      viewModel.ShowType = false;
    }

    if (!searchProviderCapabilities.CanReportScore)
    {
      viewModel.ShowScore = false;
      viewModel.ShowScoreAssessment = false;
    }

    if (!searchProviderCapabilities.CanReportSize)
    {
      viewModel.ShowSize = false;
    }

    if (!searchProviderCapabilities.CanReportType)
    {
      viewModel.ShowType = false;
    }

    if (!searchProviderCapabilities.CanReportMatchedTerms)
    {
      viewModel.IncludeUrlTextFragment = false;
    }
  }

  private string GetFriendlyName(System.Type type)
  {
    System.ComponentModel.DisplayNameAttribute displayNameAttribute = type.GetCustomAttributes<System.ComponentModel.DisplayNameAttribute>(false)
      .FirstOrDefault();

    if (displayNameAttribute == null)
    {
      return $"{type.FullName}";
    }
    else
    {
      return displayNameAttribute.DisplayName;
    }
  }

}