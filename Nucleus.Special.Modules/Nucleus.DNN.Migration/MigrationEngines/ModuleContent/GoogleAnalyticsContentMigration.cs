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

public class GoogleAnalyticsContentMigration : ModuleContentMigrationBase
{
  private ISiteManager SiteManager { get; }
  private const string SETTING_ANALYTICS_ID = "googleanalytics:id";

  public GoogleAnalyticsContentMigration(ISiteManager siteManager)
  {
    this.SiteManager = siteManager;
  }

  public override string ModuleFriendlyName => "Google Analytics";

  public override Guid ModuleDefinitionId => Guid.Empty;

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "DNNStuff - Google Analytics" };
    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    string analyticsId = null;
    Site site = await this.SiteManager.Get(newPage.SiteId);

    site.SiteSettings.TryGetValue(SETTING_ANALYTICS_ID, out string existingId);

    // only migrate analytics id if it is not already set
    if (String.IsNullOrEmpty(existingId))
    {
      foreach (Models.DNN.PageModuleSetting setting in dnnModule.Settings)
      {
        switch (setting.SettingName)
        {
          case "AccountNumber":
            analyticsId = setting.SettingValue;
            break;
        }
      }

      if (!String.IsNullOrEmpty(analyticsId))
      {
        site.SiteSettings.TrySetValue(SETTING_ANALYTICS_ID, analyticsId);
        await this.SiteManager.Save(site);
      }
    }
  }
}

