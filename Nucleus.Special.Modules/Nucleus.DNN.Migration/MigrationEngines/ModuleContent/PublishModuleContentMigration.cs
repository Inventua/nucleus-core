using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class PublishModuleContentMigration : ModuleContentMigrationBase
{
  private DNNMigrationManager DnnMigrationManager { get; }

  public PublishModuleContentMigration(DNNMigrationManager dnnMigrationManager)
  {
    this.DnnMigrationManager = dnnMigrationManager;
  }

  public override string ModuleFriendlyName => "Publish";

  public override Guid ModuleDefinitionId => new("20af00b8-1d72-4c94-bce7-b175e0b173af");

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "Blog" };

    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    Nucleus.Abstractions.Portable.IPortable portable = this.DnnMigrationManager.GetPortableImplementation(this.ModuleDefinitionId);
    List<Models.DNN.Modules.Blog> blogs = await this.DnnMigrationManager.ListDnnBlogs(dnnPage.PortalId.Value);

    foreach (Models.DNN.Modules.Blog blog in blogs)
    {
      foreach (Models.DNN.Modules.BlogEntry entry in blog.BlogEntries)
      {
        object newArticle = new
        {
          Title = entry.Title,
          SubTitle = "",
          Description = "",
          Summary = StripHtml(System.Web.HttpUtility.HtmlDecode(entry.Description)),
          Body = System.Web.HttpUtility.HtmlDecode(entry.Entry),
          PublishDate = entry.AddedDate,
          Enabled = entry.Published,
          Featured = false
        };

        await portable.Import(newModule, new List<object> { newArticle });
      }
    }
  }

  /// <summary>
  /// Remove HTML tags from string using char array.
  /// </summary>
  public static string StripHtml(string source)
  {
    char[] array = new char[source.Length];
    int arrayIndex = 0;
    bool inside = false;

    for (int i = 0; i < source.Length; i++)
    {
      char let = source[i];
      if (let == '<')
      {
        inside = true;
        continue;
      }
      if (let == '>')
      {
        inside = false;
        continue;
      }
      if (!inside)
      {
        array[arrayIndex] = let;
        arrayIndex++;
      }
    }
    return new string(array, 0, arrayIndex);
  }
}
