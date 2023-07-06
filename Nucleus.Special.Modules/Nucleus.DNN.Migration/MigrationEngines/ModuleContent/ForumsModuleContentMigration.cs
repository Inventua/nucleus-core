using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Managers;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class ForumsModuleContentMigration : ModuleContentMigrationBase
{
  private DNNMigrationManager DnnMigrationManager { get; }
  private ISiteManager SiteManager { get; }
  private IFileSystemManager FileSystemManager { get; }

  public ForumsModuleContentMigration(DNNMigrationManager dnnMigrationManager, ISiteManager siteManager, IFileSystemManager fileSystemManager)
  {
    this.DnnMigrationManager = dnnMigrationManager;
    this.SiteManager = siteManager;
    this.FileSystemManager = fileSystemManager;
  }

  public override string ModuleFriendlyName => "Forums";

  public override Guid ModuleDefinitionId => new("ea9b5d66-b791-414c-8c52-a20536cfa9f5");

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "NTForums" };

    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    Site site = await this.SiteManager.Get(newPage.SiteId);
    Nucleus.Abstractions.Portable.IPortable portable = this.DnnMigrationManager.GetPortableImplementation(this.ModuleDefinitionId);

    // Nucleus.Abstractions.Models.List categoriesList = null;
    FileSystemProviderInfo fileSystemProvider = this.FileSystemManager.ListProviders().FirstOrDefault();

    foreach (Models.DNN.Modules.ForumGroup dnnGroup in await this.DnnMigrationManager.ListDnnForumGroups(dnnModule.ModuleId))
    {
      List<object> forums = new();

      foreach (Models.DNN.Modules.Forum dnnForum in dnnGroup.Forums)
      {
        object newForumSettings = new
        {
          Enabled = dnnForum.Active,
          Visible = !dnnForum.Hidden,
          IsModerated = dnnForum.IsModerated,
          AllowAttachments = !dnnForum.AttachCount.HasValue ? false : dnnForum.AttachCount.Value > 0,
          AllowSearchIndexing = dnnForum.IndexContent
        };

        object newForum = new
        {
          _type = "Forum",
          Name = dnnForum.Name,
          Description = dnnForum.Description,
          SortOrder = dnnForum.SortOrder,
          Settings = newForumSettings,
          UseGroupSettings = dnnForum.InheritGroupSettings
        };
        // Permissions, Statistics 
        forums.Add(newForum);
      }

      object newGroupSettings = new
      {
        Enabled = dnnGroup.Settings.Active,
        Visible = !dnnGroup.Settings.Hidden,
        IsModerated = dnnGroup.Settings.IsModerated,
        AllowAttachments = !dnnGroup.Settings.AttachCount.HasValue ? false : dnnGroup.Settings.AttachCount.Value > 0,
        AllowSearchIndexing = dnnGroup.Settings.IndexContent
      };

      object newGroup = new
      {
        _type = "ForumGroup",
        ModuleId = newModule.Id,
        Name = dnnGroup.Name,
        SortOrder = dnnGroup.SortOrder,
        Settings = newGroupSettings,
        Forums = forums
      };
      // Permissions 

      await portable.Import(newModule, new List<object> { newGroup });
    }

  }
}
