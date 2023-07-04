using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Extensions;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.FileSystemProviders;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class LinksModuleContentMigration : ModuleContentMigrationBase
{
  private DNNMigrationManager DnnMigrationManager { get; }
  private IListManager ListManager { get; }
  private IFileSystemManager FileSystemManager { get; }
  private ISiteManager SiteManager { get; }

  private const string MODULESETTING_CATEGORYLIST_ID = "links:categorylistid";
  private const string MODULESETTING_LAYOUT = "links:layout";
  private const string MODULESETTING_OPEN_NEW_WINDOW = "links:opennewwindow";
  private const string MODULESETTING_SHOW_IMAGES = "links:showimages";

  public LinksModuleContentMigration(DNNMigrationManager dnnMigrationManager, ISiteManager siteManager, IListManager listManager, IFileSystemManager fileSystemManager)
  {
    this.ListManager = listManager;
    this.SiteManager = siteManager;
    this.FileSystemManager = fileSystemManager;
    this.DnnMigrationManager = dnnMigrationManager;
  }

  public override Guid ModuleDefinitionId => new("b516d8dd-c793-4776-be33-902eb704bef6");

  public override string ModuleFriendlyName => "Links";

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "links", "dnn_links" };

    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  // TODO:
  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    //Site site = await this.SiteManager.Get(newPage.SiteId);

    //Nucleus.Abstractions.Models.List categoriesList = null;
    //FileSystemProviderInfo fileSystemProvider = this.FileSystemManager.ListProviders().FirstOrDefault();

    // migrate settings
   
    //newModule.ModuleSettings.Set(MODULESETTING_ALLOWSORTING, settings.AllowUserSort);

    //List<Models.DNN.Modules.Link> contentSource = await this.DnnMigrationManager.ListDnnLinks(dnnModule.ModuleId);
    //foreach (Nucleus.DNN.Migration.Models.DNN.Modules.Link dnnLink in contentSource)
    //{
    //  Nucleus.DNN.Migration.Models.Nucleus.Link newLink = null;

    //  newLink = await this.DnnMigrationManager.GetLink(site, newModule, dnnLink.Title);

    //  if (newLink == null)
    //  {
    //    newLink = new();
    //    newLink.SortOrder = dnnLink.ViewOrder;
    //  }

    //  newLink.Title = dnnLink.Title;
    //  newLink.Description = dnnLink.Description;

    //  if (categoriesList != null)
    //  {
    //    newLink.Category = categoriesList.Items
    //      .Where(item => item.Name.Equals(dnnLink.Category, StringComparison.OrdinalIgnoreCase))
    //      .FirstOrDefault();
    //  }

    //  if (dnnLink.Url.StartsWith("FileID=", StringComparison.OrdinalIgnoreCase))
    //  {
    //    // link is a file
    //    Models.DNN.File dnnLinkFile = await this.DnnMigrationManager.GetDNNFile(Int32.Parse(dnnLink.Url.Replace("fileid=", "", StringComparison.OrdinalIgnoreCase)));

    //    try
    //    {
    //      Nucleus.Abstractions.Models.FileSystem.File newFile = await this.FileSystemManager.GetFile(site, fileSystemProvider.Key, dnnLinkFile.Path());
    //      newLink.File = newFile;
    //    }
    //    catch (FileNotFoundException)
    //    {
    //      dnnPage.AddWarning($"Link '{dnnLink.Title}' in links module '{dnnModule.ModuleTitle}' was not migrated because its file could not be found.");
    //      break;
    //    }
    //  }
    //  else
    //  {
    //    // unsupported type.  The Nucleus links module does not support Urls/anything but files
    //    dnnPage.AddWarning($"Link '{dnnLink.Title}' in links module '{dnnModule.ModuleTitle}' was not migrated because it does not reference a file.");
    //    break;
    //  }

    //  await this.DnnMigrationManager.SaveLink(newModule, newLink);
    //};
  }
}
