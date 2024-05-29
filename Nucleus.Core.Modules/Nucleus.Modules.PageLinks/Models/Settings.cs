using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Modules.PageLinks.Models;

public class Settings
{
  private class ModuleSettingsKeys
  {
    public const string MODULESETTING_TITLE = "pagelinks:title";
    public const string MODULESETTING_MODE = "pagelinks:mode";
    public const string MODULESETTING_INCLUDE_HEADERS = "pagelinks:include-headers";
    public const string MODULESETTING_HEADING_CLASS = "pagelinks:heading-class";
    public const string MODULESETTING_ROOT_SELECTOR = "pagelinks:root-selector";
  }

  public enum Modes
  {
    Automatic = 0,
    Manual = 1
  }

  [Flags]
  public enum HtmlHeaderTags
  {
    [Display(Name = "Heading 1 (H1)", Description = "Include H1 elements in automatically generated page links.")] H1 = 1,
    [Display(Name = "Heading 2 (H2)", Description = "Include H2 elements in automatically generated page links.")] H2 = 2,
    [Display(Name = "Heading 3 (H3)", Description = "Include H3 elements in automatically generated page links.")] H3 = 4,
    [Display(Name = "Heading 4 (H4)", Description = "Include H4 elements in automatically generated page links.")] H4 = 8,
    [Display(Name = "Heading 5 (H5)", Description = "Include H5 elements in automatically generated page links.")] H5 = 16,
    [Display(Name = "Heading 6 (H6)", Description = "Include H6 elements in automatically generated page links.")] H6 = 32
  }

  public string Title { get; set; }
  public Modes Mode { get; set; }
  public Dictionary<HtmlHeaderTags, Boolean> IncludeHeaders { get; set; } = [];

  public string HeadingClass { get; set; }
  public string RootSelector { get; set; }
  
  public void GetSettings(PageModule module)
  {
    this.Title = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_TITLE, "");
    this.Mode = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_MODE, Modes.Automatic);
    this.HeadingClass = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_HEADING_CLASS, "");
    this.RootSelector = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_ROOT_SELECTOR, "");
    this.IncludeHeaders = GetIncludeHeadersDictionary(module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_INCLUDE_HEADERS, HtmlHeaderTags.H1 | HtmlHeaderTags.H2 | HtmlHeaderTags.H3 | HtmlHeaderTags.H4 | HtmlHeaderTags.H5 | HtmlHeaderTags.H6));
  }

  public void SetSettings(PageModule module)
  {
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_TITLE, this.Title);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_MODE, this.Mode);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_HEADING_CLASS, this.HeadingClass);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_ROOT_SELECTOR, this.RootSelector);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_INCLUDE_HEADERS, GetIncludeHeadersFromDictionary());
  }

  private Dictionary<HtmlHeaderTags, Boolean> GetIncludeHeadersDictionary(HtmlHeaderTags value)
  {
    Dictionary<HtmlHeaderTags, Boolean> result = [];
    foreach (HtmlHeaderTags htmlHeaderTag in Enum.GetValues(typeof(HtmlHeaderTags)))
    {
      result.Add(htmlHeaderTag, value.HasFlag(htmlHeaderTag));
    }

    return result;
  }

  private HtmlHeaderTags GetIncludeHeadersFromDictionary()
  {
    HtmlHeaderTags result = 0;

    foreach (HtmlHeaderTags htmlHeaderTag in Enum.GetValues(typeof(HtmlHeaderTags)))
    {
      if (this.IncludeHeaders[htmlHeaderTag])
      {
        result |= htmlHeaderTag;
      }
      else
      {
        result &= ~htmlHeaderTag;
      }
    }

    return result;
  }
}
