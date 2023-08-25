using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.IFrame.Models;
public class Settings
{
  public enum ScrollingOptions
  {
    Auto = 0,
    Yes = 1,
    No = 2
  }

  private class ModuleSettingsKeys
  {
    public const string MODULESETTING_URL = "iframe:url";
    public const string MODULESETTING_NAME = "iframe:name";
    public const string MODULESETTING_TITLE = "iframe:title";
    public const string MODULESETTING_WIDTH = "iframe:width";
    public const string MODULESETTING_HEIGHT = "iframe:height";
    public const string MODULESETTING_SCROLLING = "iframe:scrolling";
    public const string MODULESETTING_BORDER = "iframe:border";
  }

  public string Url { get; set; }
  
  public string Title { get; set; }

  public string Name { get; set; }

  public string Width { get; set; }
  
  public string Height { get; set; }
  
  public Boolean Border { get; set; }
  

  public ScrollingOptions Scrolling { get; set; }

  public void GetSettings(PageModule module)
  {
    this.Url = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_URL, "");
    this.Title = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_TITLE, "");
    this.Name = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_NAME, "");
    this.Width= module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_WIDTH, "");
    this.Height= module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_HEIGHT, "");

    this.Border= module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_BORDER, false);

    this.Scrolling = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SCROLLING, ScrollingOptions.Auto);
  }

  public void SetSettings(PageModule module)
  {
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_URL, this.Url);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_TITLE, this.Title);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_NAME, this.Name );
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_WIDTH, this.Width );
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_HEIGHT, this.Height);

    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_BORDER, this.Border);

    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SCROLLING, this.Scrolling );
  }
}

