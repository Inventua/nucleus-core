using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

internal class IFrameContentMigration : ModuleContentMigrationBase
{
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

  private class DnnIFrameSettingsKeys
  {
    public const string MODULESETTING_URL = "src";
    public const string MODULESETTING_NAME = "name";
    public const string MODULESETTING_TITLE = "title";
    public const string MODULESETTING_WIDTH = "width";
    public const string MODULESETTING_HEIGHT = "height";
    
    public const string MODULESETTING_SCROLLING = "scrolling";
    public const string MODULESETTING_BORDER = "border";
  }

  public IFrameContentMigration()
  {
  }

  public override string ModuleFriendlyName => "IFrame";

  public override Guid ModuleDefinitionId => new("505e91b2-df6b-49b9-b66e-770ed68db047");

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "DNN_IFRAME" };
    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  public override Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {    
    foreach (Models.DNN.PageModuleSetting setting in dnnModule.Settings)
    {
      switch (setting.SettingName)
      {
        case DnnIFrameSettingsKeys.MODULESETTING_URL:
          newModule.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_URL, setting.SettingValue);
          break;

        case DnnIFrameSettingsKeys.MODULESETTING_TITLE:
          newModule.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_TITLE, setting.SettingValue);
          break;

        case DnnIFrameSettingsKeys.MODULESETTING_NAME:
          newModule.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_NAME, setting.SettingValue);
          break;

        case DnnIFrameSettingsKeys.MODULESETTING_HEIGHT:
          newModule.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_HEIGHT, String.IsNullOrEmpty(setting.SettingValue) ? "" : setting.SettingValue + "px");
          break;

        case DnnIFrameSettingsKeys.MODULESETTING_WIDTH:
          newModule.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_WIDTH, String.IsNullOrEmpty(setting.SettingValue) ? "" : setting.SettingValue + "px");
          break;

        case DnnIFrameSettingsKeys.MODULESETTING_BORDER:
          newModule.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_BORDER, setting.SettingValue == "yes");
          break;

        case DnnIFrameSettingsKeys.MODULESETTING_SCROLLING:
          newModule.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SCROLLING, setting.SettingValue);
          break;
      }
    }

    return Task.CompletedTask;
  }
}
