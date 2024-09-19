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
  private class ModuleSettingsKeys
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
  public Guid ResultsPageId { get; set; }
  public string Prompt { get; set; } = PROMPT_DEFAULT;

  public DisplayModes DisplayMode { get; set; }
  public SearchModes SearchMode { get; set; }
  public string SearchCaption { get; set; }
  public string SearchButtonCaption { get; set; }

  public Boolean IncludeUrlTextFragment { get; set; }
  public Boolean IncludeFiles { get; set; } = true;
  public string IncludeScopes { get; set; }
  public int MaximumSuggestions { get; set; }

  public Boolean ShowUrl { get; set; }
  public Boolean ShowSummary { get; set; }
  public Boolean ShowCategories { get; set; }
  public Boolean ShowPublishDate { get; set; }

  public Boolean ShowSize { get; set; }
  public Boolean ShowScore { get; set; }
  public Boolean ShowScoreAssessment { get; set; }
  public Boolean ShowType { get; set; }


  public void GetSettings(PageModule module)
  {
    this.SearchProvider = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SEARCHPROVIDER, "");
    this.ResultsPageId = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_RESULTS_PAGE, Guid.Empty);
    this.DisplayMode = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_DISPLAY_MODE, DisplayModes.Full);
    this.SearchCaption = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SEARCH_CAPTION, "Search Term");
    this.Prompt = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SEARCH_PROMPT, PROMPT_DEFAULT);
    this.SearchButtonCaption = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SEARCH_BUTTON_CAPTION, "Search");
    this.IncludeFiles = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_INCLUDE_FILES, true);

    this.ShowUrl = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOW_URL, false);
    this.ShowSummary = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOW_SUMMARY, true);
    this.ShowCategories = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOW_CATEGORIES, true);
    this.ShowPublishDate = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOW_PUBLISHEDDATE, true);

    this.ShowType = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOW_TYPE, true);
    this.ShowSize = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOW_SIZE, true);
    this.ShowScore = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOW_SCORE, true);
    this.ShowScoreAssessment = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOW_SCORE_ASSESSMENT, true);
    this.IncludeUrlTextFragment = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_INCLUDE_URL_TEXT_FRAGMENT, true);

    this.IncludeScopes = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_INCLUDE_SCOPES, "");
    this.MaximumSuggestions = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_MAXIMUM_SUGGESTIONS, 5);
    this.SearchMode = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SEARCH_MODE, SearchModes.Any);

    this.SearchButtonCaption = String.IsNullOrWhiteSpace(this.SearchButtonCaption) ? "Search" : this.SearchButtonCaption;
  }

  public void SetSettings(PageModule module)
  {
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SEARCHPROVIDER, this.SearchProvider);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_RESULTS_PAGE, this.ResultsPageId);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_DISPLAY_MODE, this.DisplayMode);

    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SEARCH_CAPTION, this.SearchCaption);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SEARCH_PROMPT, this.Prompt);

    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SEARCH_BUTTON_CAPTION, this.SearchButtonCaption);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_INCLUDE_FILES, this.IncludeFiles);

    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOW_URL, this.ShowUrl);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOW_SUMMARY, this.ShowSummary);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOW_CATEGORIES, this.ShowCategories);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOW_PUBLISHEDDATE, this.ShowPublishDate);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_INCLUDE_URL_TEXT_FRAGMENT, this.IncludeUrlTextFragment);

    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOW_TYPE, this.ShowType);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOW_SIZE, this.ShowSize);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOW_SCORE, this.ShowScore);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOW_SCORE_ASSESSMENT, this.ShowScoreAssessment);

    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_INCLUDE_SCOPES, this.IncludeScopes);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_MAXIMUM_SUGGESTIONS, this.MaximumSuggestions);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SEARCH_MODE, this.SearchMode);
  }
}
