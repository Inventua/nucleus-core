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

  public override string ModuleFriendlyName => "Text/HTML";

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
    Models.DNN.Modules.TextHtml contentSource = await this.DnnMigrationManager.GetDnnHtmlContent(dnnModule.ModuleId);
    Nucleus.Abstractions.Models.Content content;

    content = (await this.ContentManager.List(newModule)).FirstOrDefault();
    
    if (content == null)
    {
      content = new();
    }
    
    if (contentSource != null)
    {
      content.SortOrder = 10;
      content.ContentType = "text/html";
      content.Value = System.Web.HttpUtility.HtmlDecode(contentSource.Content);

      // apply "guesses" to the content to help with images/other file links.  This assumes that the contents of /Portals/[index] will be copied in the same directory structure
      // to Nucleus
      content.Value = content.Value.Replace($"Portals/{dnnPage.PortalId}/", "");

      // rewrite url links like https://site.com/LinkClick.aspx?link=54&tabid=166
      // TODO


      // <img alt="Microsoft Certified Partner" style="border-right: white 15px solid; border-top: white 10px solid; border-left: white 15px solid; border-bottom: white 4px solid" src="/Portals/0/Images/MS-CertifiedPartner.jpg">
      await this.ContentManager.Save(newModule, content);
    }
  }
}
