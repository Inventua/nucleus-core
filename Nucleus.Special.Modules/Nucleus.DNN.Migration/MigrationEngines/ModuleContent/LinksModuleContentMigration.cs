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
  private IPageManager PageManager { get; }

  private IFileSystemManager FileSystemManager { get; }
  private ISiteManager SiteManager { get; }

  private const string MODULESETTING_CATEGORYLIST_ID = "links:categorylistid";
  private const string MODULESETTING_LAYOUT = "links:layout";
  private const string MODULESETTING_OPEN_NEW_WINDOW = "links:opennewwindow";
  private const string MODULESETTING_SHOW_IMAGES = "links:showimages";

  public LinksModuleContentMigration(DNNMigrationManager dnnMigrationManager, ISiteManager siteManager, IPageManager pageManager, IListManager listManager, IFileSystemManager fileSystemManager)
  {
    this.PageManager = pageManager;
    this.ListManager = listManager;
    this.SiteManager = siteManager;
    this.FileSystemManager = fileSystemManager;
    this.DnnMigrationManager = dnnMigrationManager;
  }

  public override Guid ModuleDefinitionId => new("374e62b5-024d-4d8d-95a2-e56f476fe887");

  public override string ModuleFriendlyName => "Links";

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "links", "dnn_links" };

    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    Site site = await this.SiteManager.Get(newPage.SiteId);
    Nucleus.Abstractions.Portable.IPortable portable = this.DnnMigrationManager.GetPortableImplementation(this.ModuleDefinitionId);

    FileSystemProviderInfo fileSystemProvider = this.FileSystemManager.ListProviders().FirstOrDefault();

    // there are no settings in DNN links that can be migrated to Nucleus

    List<Models.DNN.Modules.Link> contentSource = await this.DnnMigrationManager.ListDnnLinks(dnnModule.ModuleId);
    foreach (Nucleus.DNN.Migration.Models.DNN.Modules.Link dnnLink in contentSource)
    {
      Nucleus.Abstractions.Models.FileSystem.File newLinkFile = null;
      Nucleus.Abstractions.Models.Page newLinkPage = null;
      Boolean doSave = true;

      if (dnnLink.Url.StartsWith("FileID=", StringComparison.OrdinalIgnoreCase))
      {
        // link is a file
        Models.DNN.File dnnLinkFile = await this.DnnMigrationManager.GetDNNFile(Int32.Parse(dnnLink.Url.Replace("fileid=", "", StringComparison.OrdinalIgnoreCase)));

        try
        {
          newLinkFile = await this.FileSystemManager.GetFile(site, fileSystemProvider.Key, dnnLinkFile.Path());
        }
        catch (System.IO.FileNotFoundException)
        {
          dnnPage.AddWarning($"Link '{dnnLink.Title}' in links module '{dnnModule.ModuleTitle}' was not migrated because its file '{dnnLinkFile.Path()}' could not be found.");
          break;
        }
      }
      else if (int.TryParse(dnnLink.Url, out int linkPageId))
      {
        // link is a page
        if (createdPagesKeys.ContainsKey(linkPageId))
        {
          newLinkPage = await this.PageManager.Get(createdPagesKeys[linkPageId]);
          if (newLinkPage == null)
          {
            dnnPage.AddWarning($"Unable to set the page property for a migrated link for link '{dnnLink.Title}', module '{dnnModule.ModuleTitle}'.  A page with Id '{createdPagesKeys[linkPageId]}' was not found.");
          }
        }
        else
        {
          dnnPage.AddWarning($"Unable to set the root page setting for site map module '{dnnModule.ModuleTitle}'.  DNN page with id '{linkPageId}' has not been migrated.");
        }
      }
      else if (dnnLink.Url.StartsWith("http"))
      {
        // link is an Url
      }
      else
      {
        // User link, or other unrecognized link
        dnnPage.AddWarning($"Link '{dnnLink.Title}' in links module '{dnnModule.ModuleTitle}' was not migrated because it is a user or other unsupported link type.");
        doSave = false;
      }

      if (doSave)
      {
        object newLink;

        if (newLinkFile != null)
        {
          newLink = new
          {
            Title = dnnLink.Title,
            Description = dnnLink.Description,
            LinkType = Nucleus.Abstractions.Models.FileSystem.File.URN,
            SortOrder = dnnLink.ViewOrder,
            LinkFile = new { File = newLinkFile }
          };
        }
        else if (newLinkPage != null)
        {
          newLink = new
          {
            Title = dnnLink.Title,
            Description = dnnLink.Description,
            LinkType = Nucleus.Abstractions.Models.Page.URN,
            SortOrder = dnnLink.ViewOrder,
            LinkPage = new { Page = newLinkPage }
          };
        }
        else
        {
          newLink = new
          {
            Title = dnnLink.Title,
            Description = dnnLink.Description,
            LinkType = "urn:url",
            SortOrder = dnnLink.ViewOrder,
            LinkUrl = new { Url = dnnLink.Url },
            File = newLinkFile,
          };
        }

        await portable.Import(newModule, new Nucleus.Abstractions.Portable.PortableContent("urn:nucleus:entities:link", newLink ));
      }
    };
  }
}
