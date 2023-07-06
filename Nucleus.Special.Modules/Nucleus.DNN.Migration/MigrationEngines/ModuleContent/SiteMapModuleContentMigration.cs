using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Wordprocessing;
using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Microsoft.AspNetCore.Routing;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class SiteMapModuleContentMigration : ModuleContentMigrationBase
{
  private const string SETTINGS_MAXLEVELS = "sitemap:maxlevels";
  private const string SETTINGS_ROOTPAGE_TYPE = "sitemap:root-page-type";
  private const string SETTINGS_ROOTPAGE = "sitemap:root-page";
  private const string SETTINGS_SHOWDESCRIPTION = "sitemap:show-description";
  private const string SETTINGS_USENAME = "sitemap:use-name";
  private const string SETTINGS_DIRECTION = "sitemap:direction";

  private enum RootPageTypes
  {
    SiteRoot = 0,
    SelectedPage = 1,
    HomePage = 2,
    CurrentPage = 3,
    ParentPage = 4,
    TopAncestor = 5,
    Dual = 6
  }

  private enum Directions
  {
    Vertical = 0,
    Horizontal = 1
  }

  private ISiteManager SiteManager { get; }
  private IPageManager PageManager { get; }

  public SiteMapModuleContentMigration(ISiteManager siteManager, IPageManager pageManager)
  {
    this.SiteManager = siteManager;
    this.PageManager = pageManager;
  }

  public override Guid ModuleDefinitionId => new("0392bf73-c646-4ccc-bcb5-372a75b9ea84"); 
  public override string ModuleFriendlyName => "Sitemap";

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "console", "sitemap", "Inventua - TopMenu", "Inventua - SideMenu" };

    return (matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase));
  }

  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    Site site = await this.SiteManager.Get(newPage.SiteId);

    switch (dnnModule.DesktopModule.ModuleName)
    {
      case "Inventua - TopMenu":
        newModule.ModuleSettings.Set(SETTINGS_DIRECTION, Directions.Horizontal);
        break;
      case "Inventua - SideMenu":
        newModule.ModuleSettings.Set(SETTINGS_DIRECTION, Directions.Vertical);
        break;
      default:
        break;
    }

    foreach (var setting in dnnModule.Settings)
    {
      switch (setting.SettingName)
      {
        case string settingName when String.Equals(settingName, "Levels", StringComparison.OrdinalIgnoreCase):
          if (int.TryParse(setting.SettingValue, out int levelsValue))
          {
            if (levelsValue > 1)
            {
              switch (dnnModule.DesktopModule.ModuleName)
              {
                // The TopMenu and SideMenu "levels" value is one more than it should be
                case "Inventua - TopMenu":
                case "Inventua - SideMenu":
                  levelsValue -= 1;
                  break;
              }
            }
            newModule.ModuleSettings.Set(SETTINGS_MAXLEVELS, levelsValue);
          }
          break;

        case string settingName when String.Equals(settingName, "selectedTab", StringComparison.OrdinalIgnoreCase):
          if (int.TryParse(setting.SettingValue, out int selectedTabId))
          {
            if (createdPagesKeys.ContainsKey(dnnPage.PageId))
            {
              Nucleus.Abstractions.Models.Page page = await this.PageManager.Get(createdPagesKeys[dnnPage.PageId]);
              if (page != null)
              {
                newModule.ModuleSettings.Set(SETTINGS_ROOTPAGE, page.Id);
              }
              else
              {
                dnnPage.AddWarning($"Unable to set the root page setting for site map module '{dnnModule.ModuleTitle}'.  A page with Id '{createdPagesKeys[dnnPage.PageId]}' was not found.");
              }
            }
            else
            {
              dnnPage.AddWarning($"Unable to set the root page setting for site map module '{dnnModule.ModuleTitle}'.  DNN page with id '{dnnPage.PageId}' has not been migrated.");
            }
          }
          break;

        case string settingName when String.Equals(settingName, "Source", StringComparison.OrdinalIgnoreCase):
          if (int.TryParse(setting.SettingValue, out int sourceValue))
          {
            switch (sourceValue)
            {
              case -1:
                newModule.ModuleSettings.Set(SETTINGS_ROOTPAGE_TYPE, RootPageTypes.SiteRoot);
                break;
              case -2:
                newModule.ModuleSettings.Set(SETTINGS_ROOTPAGE_TYPE, RootPageTypes.CurrentPage);
                break;
              case -3:
                newModule.ModuleSettings.Set(SETTINGS_ROOTPAGE_TYPE, RootPageTypes.ParentPage);
                break;
              case -4:
                newModule.ModuleSettings.Set(SETTINGS_ROOTPAGE_TYPE, RootPageTypes.TopAncestor);
                break;
              case -5:
                newModule.ModuleSettings.Set(SETTINGS_ROOTPAGE_TYPE, RootPageTypes.Dual);
                break;
            }
           
          }
          break;

        case string settingName when String.Equals(settingName, "CurrentSubTreeOnly", StringComparison.OrdinalIgnoreCase):
          // this setting is not implemented in the Nucleus SiteMap module
          break;

        case string settingName when String.Equals(settingName, "ShowDescriptions", StringComparison.OrdinalIgnoreCase):
          switch (setting.SettingValue)
          {
            case "none":
              newModule.ModuleSettings.Set(SETTINGS_SHOWDESCRIPTION, false);
              break;
            default:
              if (Boolean.TryParse(setting.SettingValue, out Boolean showDescriptionsValue))
              {
                newModule.ModuleSettings.Set(SETTINGS_SHOWDESCRIPTION, showDescriptionsValue);
              }
              break;
          }          
          break;

        case string settingName when String.Equals(settingName, "UseName", StringComparison.OrdinalIgnoreCase):
          // the Nucleus sitemap module doesn't currently support "use name", but this is likely to be a 
          // future enhancement
          newModule.ModuleSettings.Set(SETTINGS_USENAME, setting.SettingValue);
          break;
      }
    }
  }
}
