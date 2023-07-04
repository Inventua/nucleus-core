using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using Nucleus.Abstractions.FileSystemProviders;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class HtmlModuleContentMigration : ModuleContentMigrationBase
{
  private IPageManager PageManager { get; }
  private IFileSystemManager FileSystemManager { get; }
  private IContentManager ContentManager { get; }
  private DNNMigrationManager DnnMigrationManager { get; }

  public HtmlModuleContentMigration(IContentManager contentManager, IPageManager pageManager, IFileSystemManager fileSystemManager, DNNMigrationManager dnnMigrationManager)
  {
    this.PageManager = pageManager;
    this.ContentManager = contentManager;
    this.FileSystemManager = fileSystemManager;
    this.DnnMigrationManager = dnnMigrationManager;
  }

  public override Guid ModuleDefinitionId => new("b516d8dd-c793-4776-be33-902eb704bef6");

  public override string ModuleFriendlyName => "Text/HTML";

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    return desktopModule.ModuleName.Equals("dnn_html", StringComparison.OrdinalIgnoreCase);
  }

  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    Models.DNN.Modules.TextHtml contentSource = await this.DnnMigrationManager.GetDnnHtmlContent(dnnModule.ModuleId);
    Nucleus.Abstractions.Models.Content content;
    FileSystemProviderInfo fileSystemProvider = this.FileSystemManager.ListProviders().FirstOrDefault();

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

      // apply "guesses" to the content to help with images/other file links.  This assumes that the contents of
      // /Portals/[index] will be copied in the same directory structure to Nucleus
      content.Value = content.Value.Replace($"/Portals/{dnnPage.PortalId}/", $"/files/{fileSystemProvider.Key}/");

      // rewrite url links like https://site.com/LinkClick.aspx?link=54&tabid=166
      foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(content.Value, "/LinkClick\\.aspx\\?link=(?<linkid>[0-9]*)&tabid=(?<tabid>[0-9]*)"))
      {
        if (match.Success)
        {
          string linkId = match.Groups["linkid"].Value;
          string tabId = match.Groups["tabid"].Value;

          if (createdPagesKeys.ContainsKey(int.Parse(tabId)))
          {
            Nucleus.Abstractions.Models.Page page = await this.PageManager.Get(createdPagesKeys[int.Parse(tabId)]);
            string path = page.DefaultPageRoute().Path;
            path += path.EndsWith("/") ? "" : "/";

            content.Value = content.Value.Replace($"/LinkClick.aspx?link={linkId}&tabid={tabId}", path, StringComparison.OrdinalIgnoreCase);
          }
        }
      }

      await this.ContentManager.Save(newModule, content);
    }
  }
}
