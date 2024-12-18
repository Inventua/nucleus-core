using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.Search.Models;

public class Settings
{
  private class SettingsKeys
  {
    public const string MODULESETTING_SEARCHPROVIDER = "search:search-provider";
    public const string MODULESETTING_DISPLAY_MODE = "search:display-mode";
    public const string MODULESETTING_RESULTS_PAGE = "search:results-page";
    public const string MODULESETTING_SEARCH_CAPTION = "search:search-caption";
    public const string MODULESETTING_SEARCH_PROMPT = "search:search-prompt";
    public const string MODULESETTING_SEARCH_BUTTON_CAPTION = "search:search-button-caption";
    public const string MODULESETTING_INCLUDE_FILES = "search:include-files";

    public const string MODULESETTING_SHOW_URL = "search:show-url";
    public const string MODULESETTING_SHOW_SUMMARY = "search:show-summary";
    public const string MODULESETTING_SHOW_CATEGORIES = "search:show-categories";
    public const string MODULESETTING_SHOW_PUBLISHEDDATE = "search:show-publisheddate";

    public const string MODULESETTING_INCLUDE_URL_TEXT_FRAGMENT = "search:include-text-fragment";

    public const string MODULESETTING_SHOW_TYPE = "search:show-type";
    public const string MODULESETTING_SHOW_SIZE = "search:show-size";
    public const string MODULESETTING_SHOW_SCORE = "search:show-score";
    public const string MODULESETTING_SHOW_SCORE_ASSESSMENT = "search:show-score-assessment";

    public const string MODULESETTING_INCLUDE_SCOPES = "search:include-scopes";
    public const string MODULESETTING_MAXIMUM_SUGGESTIONS = "search:maximum-suggestions";
    public const string MODULESETTING_SEARCH_MODE = "search:search-mode";
  }

  public const string PROMPT_DEFAULT = "Search Term";

  public enum DisplayModes
  {
    Full,
    Compact,
    Minimal
  }

  public enum SearchModes
  {
    Any,
    All
  }

  public string SearchProvider { get; set; }
  public Guid ResultsPageId { get; set; } = Guid.Empty;
  public string Prompt { get; set; } = PROMPT_DEFAULT;

  public DisplayModes DisplayMode { get; set; } = DisplayModes.Full;
  public SearchModes SearchMode { get; set; } = SearchModes.Any;
  public string SearchCaption { get; set; } = "Search Term";
  public string SearchButtonCaption { get; set; } = "Search";

  public Boolean IncludeUrlTextFragment { get; set; } = true;
  public Boolean IncludeFiles { get; set; } = true;
  public string IncludeScopes { get; set; }
  public int MaximumSuggestions { get; set; } = 5;

  public Boolean ShowUrl { get; set; } = false;
  public Boolean ShowSummary { get; set; } = true;
  public Boolean ShowCategories { get; set; } = true;
  public Boolean ShowPublishDate { get; set; } = true;

  public Boolean ShowSize { get; set; } = true;
  public Boolean ShowScore { get; set; } = true;
  public Boolean ShowScoreAssessment { get; set; } = false;
  public Boolean ShowType { get; set; } = true;

  public void GetSettings(PageModule module)
  {
    this.SearchProvider = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_SEARCHPROVIDER, this.SearchProvider);
    this.ResultsPageId = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_RESULTS_PAGE, this.ResultsPageId);
    this.DisplayMode = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_DISPLAY_MODE, this.DisplayMode);
    this.SearchCaption = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_SEARCH_CAPTION, this.SearchCaption);
    this.Prompt = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_SEARCH_PROMPT, this.Prompt);
    this.SearchButtonCaption = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_SEARCH_BUTTON_CAPTION, this.SearchButtonCaption);
    this.IncludeFiles = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_INCLUDE_FILES, this.IncludeFiles);

    this.ShowUrl = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_SHOW_URL, this.ShowUrl);
    this.ShowSummary = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_SHOW_SUMMARY, this.ShowSummary);
    this.ShowCategories = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_SHOW_CATEGORIES, this.ShowCategories);
    this.ShowPublishDate = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_SHOW_PUBLISHEDDATE, this.ShowPublishDate);

    this.ShowType = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_SHOW_TYPE, this.ShowType);
    this.ShowSize = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_SHOW_SIZE, this.ShowSize);
    this.ShowScore = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_SHOW_SCORE, this.ShowScore);
    this.ShowScoreAssessment = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_SHOW_SCORE_ASSESSMENT, this.ShowScoreAssessment);
    this.IncludeUrlTextFragment = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_INCLUDE_URL_TEXT_FRAGMENT, this.IncludeUrlTextFragment);

    this.IncludeScopes = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_INCLUDE_SCOPES, this.IncludeScopes);
    this.MaximumSuggestions = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_MAXIMUM_SUGGESTIONS, this.MaximumSuggestions);
    this.SearchMode = module.ModuleSettings.Get(SettingsKeys.MODULESETTING_SEARCH_MODE, this.SearchMode);
  }

  public void SetSettings(PageModule module)
  {
    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_SEARCHPROVIDER, this.SearchProvider);
    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_RESULTS_PAGE, this.ResultsPageId);
    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_DISPLAY_MODE, this.DisplayMode);

    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_SEARCH_CAPTION, this.SearchCaption);
    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_SEARCH_PROMPT, this.Prompt);

    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_SEARCH_BUTTON_CAPTION, this.SearchButtonCaption);
    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_INCLUDE_FILES, this.IncludeFiles);

    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_SHOW_URL, this.ShowUrl);
    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_SHOW_SUMMARY, this.ShowSummary);
    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_SHOW_CATEGORIES, this.ShowCategories);
    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_SHOW_PUBLISHEDDATE, this.ShowPublishDate);
    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_INCLUDE_URL_TEXT_FRAGMENT, this.IncludeUrlTextFragment);

    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_SHOW_TYPE, this.ShowType);
    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_SHOW_SIZE, this.ShowSize);
    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_SHOW_SCORE, this.ShowScore);
    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_SHOW_SCORE_ASSESSMENT, this.ShowScoreAssessment);

    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_INCLUDE_SCOPES, this.IncludeScopes);
    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_MAXIMUM_SUGGESTIONS, this.MaximumSuggestions);
    module.ModuleSettings.Set(SettingsKeys.MODULESETTING_SEARCH_MODE, this.SearchMode);
  }
}
