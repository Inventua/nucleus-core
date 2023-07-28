using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Extensions;
using Nucleus.Abstractions.Managers;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class ShadowModuleContentMigration : ModuleContentMigrationBase
{
  public const string MODULESETTING_PAGE_ID = "shadow:page-id";
  public const string MODULESETTING_MODULE_ID = "shadow:module-id";

  private ISiteManager SiteManager { get; }
  private IPageManager PageManager { get; }
  private DNNMigrationManager DNNMigrationManager { get; }

  public ShadowModuleContentMigration(ISiteManager siteManager, IPageManager pageManager, DNNMigrationManager dnnMigrationManager)
  {
    this.SiteManager = siteManager;
    this.PageManager = pageManager;
    this.DNNMigrationManager = dnnMigrationManager;
  }

  public override string ModuleFriendlyName => "Shadow";

  public override Guid ModuleDefinitionId => new("ffaf61a9-6f15-4173-9927-532d3dedcdb5");

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    return desktopModule.ModuleName.Equals("Inventua - Shadow", StringComparison.OrdinalIgnoreCase);
  }

  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    Site site = await this.SiteManager.Get(newPage);

    string dnnPageId = dnnModule.Settings
      .Where(setting => setting.SettingName.Equals("shadow-tabid", StringComparison.OrdinalIgnoreCase))
      .FirstOrDefault()?.SettingValue;

    string dnnModuleId = dnnModule.Settings
      .Where(setting => setting.SettingName.Equals("shadow-moduleid", StringComparison.OrdinalIgnoreCase))
      .FirstOrDefault()?.SettingValue;

    if (int.TryParse(dnnPageId, out int pageId) && int.TryParse(dnnModuleId, out int moduleId))
    {
      Models.DNN.Page dnnShadowedPage = await this.DNNMigrationManager.GetDnnPage(pageId);

      if (dnnShadowedPage != null)
      {
        Nucleus.Abstractions.Models.Page nucleusShadowedPage = await this.PageManager.Get(site, dnnShadowedPage.TabPath.Replace("//", "/"));
        if (nucleusShadowedPage != null)
        {
          // set shadow page id
          newModule.ModuleSettings.Set(MODULESETTING_PAGE_ID, nucleusShadowedPage.Id);

          // set shadow module id
          Models.DNN.PageModule dnnShadowedModule = dnnShadowedPage.PageModules.Where(pageModule => pageModule.ModuleId == moduleId).FirstOrDefault();
          if (dnnShadowedModule != null)
          {
            Nucleus.Abstractions.Models.PageModule nucleusShadowedModule = nucleusShadowedPage.Modules
              .Where(module => module.Pane.Equals(dnnShadowedModule.PaneName) && module.Title.Equals(dnnShadowedModule.ModuleTitle))
              .FirstOrDefault();
            if (nucleusShadowedModule != null)
            {
              newModule.ModuleSettings.Set(MODULESETTING_MODULE_ID, nucleusShadowedModule.Id);
            }
            else
            {
              dnnPage.AddWarning($"A shadow module was created on page '{newPage.Name}' but the module id setting was not set because a Nucleus module in pane '{dnnShadowedModule.PaneName}' with title '{dnnShadowedModule.ModuleTitle}' was not found.");
            }
          }
          else
          {
            dnnPage.AddWarning($"A shadow module was created on page '{newPage.Name}' but the module id setting was not set because a DNN module with id '{moduleId}' was not found.");
          }          
        }
        else
        {
          dnnPage.AddWarning($"A shadow module was created on page '{newPage.Name}' but module settings were not set because a page with route '{dnnShadowedPage.TabPath}' was not found.");
        }
      }
      else
      {
        dnnPage.AddWarning($"A shadow module was created on page '{newPage.Name}' but module settings were not set because a DNN page with ID '{pageId}' was not found.");
      }
    }
    else
    {
      dnnPage.AddWarning($"A shadow module was created on page '{newPage.Name}' but module settings were not set because the 'shadow-tabid' or 'shadow-moduleid' setting was not found or could not be parsed.");      
    }

  }
}
