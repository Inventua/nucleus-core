using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.DNN.Migration.DataProviders;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class HtmlModuleContentMigration : ModuleContentMigrationBase
{
  private IContentManager ContentManager { get; set; }
  private DNNMigrationManager DnnMigrationManager { get; set; }

  public HtmlModuleContentMigration(IContentManager contentManager, DNNMigrationManager dnnMigrationManager)
  {
    this.ContentManager = contentManager;
    this.DnnMigrationManager = dnnMigrationManager;
  }

  public override Guid? GetMatch(IEnumerable<ModuleDefinition> modules, DesktopModule desktopModule)
  {
    if (desktopModule.ModuleName.Equals("dnn_html", StringComparison.OrdinalIgnoreCase))
    {
      return new("b516d8dd-c793-4776-be33-902eb704bef6");
    }

    return null;
  }

  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule)
  {
    Nucleus.Abstractions.Models.Content content = new();
    Models.DNN.Modules.TextHtml contentSource = await this.DnnMigrationManager.GetHtmlContent(dnnModule.TabModuleId);

    if (contentSource != null)
    {
      content.SortOrder = 10;
      content.ContentType = "text/html";
      content.Value = System.Web.HttpUtility.HtmlDecode(contentSource.Content);

      await this.ContentManager.Save(newModule, content);
    }
  }
}
