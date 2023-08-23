using DocumentFormat.OpenXml.Office2010.CustomUI;
using DocumentFormat.OpenXml.Office2016.Presentation.Command;
using Microsoft.AspNetCore.Http;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Extensions;
using Nucleus.Data.Common;
using Nucleus.DNN.Migration.DataProviders;
using Nucleus.DNN.Migration.MigrationEngines;
using Nucleus.DNN.Migration.Models;
using Nucleus.DNN.Migration.Models.DNN.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration;
/// <summary>
/// Provides functions to manage database data.
/// </summary>
public class DNNMigrationManager
{
  private IDataProviderFactory DataProviderFactory { get; }
  private IFileSystemManager FileSystemManager { get; }

  private static List<MigrationEngineBase> MigrationEngines { get; } = new();
  private IEnumerable<Nucleus.Abstractions.Portable.IPortable> PortableImplementations { get; }

  public DNNMigrationManager(IDataProviderFactory dataProviderFactory, IFileSystemManager fileSystemManager, IEnumerable<Nucleus.Abstractions.Portable.IPortable> portableImplementations)
  {
    this.DataProviderFactory = dataProviderFactory;
    this.DataProviderFactory.PreventSchemaCheck(Startup.DNN_SCHEMA_NAME);
    this.PortableImplementations = portableImplementations;
    this.FileSystemManager = fileSystemManager;
  }

  public void ClearMigrationEngines()
  {
    if (MigrationEngines.Any() && MigrationEngines.Any(engine => engine.State() == MigrationEngineBase.EngineStates.InProgress))
    {
      throw new InvalidOperationException("A migration operation is already in progress.");
    }

    MigrationEngines.Clear();
  }

  public MigrationEngineBase<TModel> GetMigrationEngine<TModel>()
    where TModel : Models.DNN.DNNEntity
  {
    return MigrationEngines.Where(engine => engine as MigrationEngineBase<TModel> != null)
      .Select(engine => engine as MigrationEngineBase<TModel>)
      .FirstOrDefault();
  }


  public List<MigrationEngineBase> GetMigrationEngines
  {
    get
    {
      return MigrationEngines;
    }
  }

  public Nucleus.Abstractions.Portable.IPortable GetPortableImplementation(Guid moduleDefinitionId)
  {
    Nucleus.Abstractions.Portable.IPortable result = this.PortableImplementations.Where(portable => portable.ModuleDefinitionId == moduleDefinitionId)
      .FirstOrDefault();

    if (result == null)
    {
      throw new InvalidOperationException($"No IPortable implementation for module definition '{moduleDefinitionId}' was found.");
    }

    return result;
  }


  public Task<MigrationEngineBase<TModel>> CreateMigrationEngine<TModel>(IServiceProvider services, List<TModel> items)
    where TModel : Models.DNN.DNNEntity
  {
    if (MigrationEngines.Any() && MigrationEngines.Any(engine => engine.State() == MigrationEngineBase.EngineStates.InProgress))
    {
      throw new InvalidOperationException("A migration operation is already in progress.");
    }

    MigrationEngineBase<TModel> engine = services.CreateEngine<TModel>();
    engine.Init(items);
    MigrationEngines.Add(engine);

    return Task.FromResult(engine);
  }


  public async Task<Models.DNN.Version> GetDnnVersion()
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      // we must handle a null provider here, which will happen if there is no configured database schema with name "DNN".  The
      // main view shows a warning message when version = null
      if (provider == null) { return null; }

      return await provider.GetVersion();
    }
  }

  #region "    Core Entities    "
  public async Task<List<Models.DNN.Portal>> ListDnnPortals()
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListPortals();
    }
  }

  public async Task<Models.DNN.RoleGroup> GetDnnRoleGroup(int id)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetRoleGroup(id);
    }
  }

  public async Task<List<Models.DNN.RoleGroup>> ListDnnRoleGroups(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListRoleGroups(portalId);
    }
  }

  public async Task<Models.DNN.Role> GetDnnRole(int id)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetRole(id);
    }
  }

  public async Task<List<Models.DNN.Role>> ListDnnRoles(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListRoles(portalId);
    }
  }

  public async Task<List<Models.DNN.List>> ListDnnLists(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListLists(portalId);
    }
  }

  public async Task<Models.DNN.File> GetDnnFile(int id)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetFile(id);
    }
  }

  public async Task<Models.DNN.User> GetDnnUser(int id)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetUser(id);
    }
  }

  public async Task<List<Models.DNN.User>> ListDnnUsers(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListUsers(portalId);
    }
  }

  public async Task<Models.DNN.Page> GetDnnPage(int id)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetPage(id);
    }
  }

  public async Task<List<Models.DNN.Page>> ListDnnPages(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListPages(portalId);
    }
  }

  public async Task<List<Models.DNN.PageUrl>> ListDnnPageUrls(int pageId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListPageUrls(pageId);
    }
  }
  #endregion 

  public async Task<Models.DNN.Modules.TextHtml> GetDnnHtmlContent(int moduleId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetHtmlContent(moduleId);
    }
  }

  public async Task<List<Models.DNN.Modules.Document>> ListDnnDocuments(int moduleId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListDocuments(moduleId);
    }
  }

  public async Task<List<Models.DNN.Modules.Link>> ListDnnLinks(int moduleId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListLinks(moduleId);
    }
  }

  public async Task<Models.DNN.Folder> GetDnnFolder(int folderId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetFolder(folderId);
    }
  }

  public async Task<List<Models.DNN.Folder>> ListDnnFolders(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return (await provider.ListFolders(portalId))
        .Where(folder => !IsReservedFolderName(folder))
        .ToList();
    }
  }

  private Boolean IsReservedFolderName(Models.DNN.Folder folder)
  {
    string[] RESERVED_PATHS = { "Cache/", "Containers/", "Skins/", "SiteMap/", "Templates/", "Users/" };

    foreach (string path in RESERVED_PATHS)
    {
      if (folder.FolderPath.StartsWith(path, StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }

  public async Task<List<Models.DNN.Modules.Blog>> ListDnnBlogs(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListBlogs(portalId);
    }
  }

  // NT Forums
  public async Task<List<Models.DNN.Modules.NTForums.ForumGroup>> ListDnnNTForumGroupsByPortal(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListNTForumGroupsByPortal(portalId);
    }
  }

  public async Task<List<Models.DNN.Modules.NTForums.ForumGroup>> ListDnnNTForumGroupsByModule(int moduleId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListNTForumGroupsByModule(moduleId);
    }
  }

  public async Task<int> CountNTForumPosts(int forumId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.CountNTForumPosts(forumId);
    }
  }

  public async Task<List<int>> ListNTForumPostIds(int forumId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListNTForumPostIds(forumId);
    }
  }

  public async Task<List<int>> ListNTForumPostReplyIds(int forumId, int postId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListNTForumPostReplyIds(forumId, postId);
    }
  }

  public async Task<Models.DNN.Modules.NTForums.ForumPost> GetNTForumPost(int postId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetNTForumPost(postId);
    }
  }

  // Active Forums
  public async Task<List<Models.DNN.Modules.ActiveForums.ForumGroup>> ListDnnActiveForumsGroupsByPortal(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      List<Models.DNN.Modules.ActiveForums.ForumGroup> results = await provider.ListActiveForumsGroupsByPortal(portalId);

      foreach (Models.DNN.Modules.ActiveForums.ForumGroup group in results)
      {
        group.Settings = await provider.ListActiveForumsSettings(group.ModuleId, group.GroupSettingsKey);

        foreach(Models.DNN.Modules.ActiveForums.Forum forum in group.Forums)
        {
          forum.Settings = await provider.ListActiveForumsSettings(group.ModuleId, forum.ForumSettingsKey);
        }
      }

      return results;
    }
  }

  public async Task<List<Models.DNN.Modules.ActiveForums.ForumGroup>> ListDnnActiveForumsGroupsByModule(int moduleId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      List<Models.DNN.Modules.ActiveForums.ForumGroup> results = await provider.ListActiveForumsGroupsByModule(moduleId);

      foreach (Models.DNN.Modules.ActiveForums.ForumGroup group in results)
      {
        group.Settings = await provider.ListActiveForumsSettings(moduleId, group.GroupSettingsKey);

        foreach (Models.DNN.Modules.ActiveForums.Forum forum in group.Forums)
        {
          forum.Settings = await provider.ListActiveForumsSettings(group.ModuleId, forum.ForumSettingsKey);
        }
      }

      return results;
    }
  }

  public async Task<int> CountActiveForumsPosts(int forumId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.CountActiveForumsTopics(forumId);
    }
  }

  public async Task<List<int>> ListActiveForumsPostIds(int forumId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListActiveForumsPostIds(forumId);
    }
  }

  public async Task<List<int>> ListActiveForumsPostReplyIds(int forumId, int postId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListActiveForumsPostReplyIds(forumId, postId);
    }
  }

  public async Task<Models.DNN.Modules.ActiveForums.ForumTopic> GetActiveForumsPost(int postId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetActiveForumsTopic(postId);
    }
  }

  public async Task<Models.ForumInfo> GetNucleusForumInfo(string groupName, string forumName)
  {
    using (DNNMigrationDataProvider provider = this.DataProviderFactory.CreateProvider<DNNMigrationDataProvider>())
    {
      return await provider.GetNucleusForumInfo(groupName, forumName);
    }
  }

  public async Task<Boolean> ForumExists(string groupName, string forumName)
  {
    using (DNNMigrationDataProvider provider = this.DataProviderFactory.CreateProvider<DNNMigrationDataProvider>())
    {
      return await provider.ForumExists(groupName, forumName);
    }
  }



  /// <summary>
  /// Retrieve document settings for the specified module.
  /// </summary>
  /// <param name="moduleId"></param>
  /// <returns></returns>
  public async Task<Models.DNN.Modules.DocumentsSettings> GetDnnDocumentsSettings(int moduleId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetDocumentsSettings(moduleId);
    }
  }

  /// <summary>
  /// Retrieve media module settings for the specified module.
  /// </summary>
  /// <param name="moduleId"></param>
  /// <returns></returns>
  public async Task<Models.DNN.Modules.MediaSettings> GetDnnMediaSettings(int moduleId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetMediaSettings(moduleId);
    }
  }


  /// <summary>
  /// Retrieve a list of skins which are in use.
  /// </summary>
  /// <param name="portalId"></param>
  /// <returns></returns>
  public async Task<List<Models.DNN.Skin>> ListSkins(int portalId)
  {
    List<Models.DNN.Skin> results = new();

    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      foreach (Models.DNN.Page page in await provider.ListPages(portalId))
      {
        if (!String.IsNullOrEmpty(page.SkinSrc))
        {
          Models.DNN.Skin skin = results
            .Where(existingSkin => existingSkin.SkinSrc.Equals(page.SkinSrc, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

          if (skin == null)
          {
            skin = new Models.DNN.Skin()
            {
              SkinSrc = page.SkinSrc,
              Panes = new()
            };

            results.Add(skin);
          }

          AddPanes(skin, page.PageModules.Select(module => module.PaneName));
        }
      }

      return results;
    }
  }

  /// <summary>
  /// Retrieve a list of DNN containers which are in use.
  /// </summary>
  /// <param name="portalId"></param>
  /// <returns></returns>
  public async Task<List<Models.DNN.Container>> ListContainers(int portalId)
  {
    List<Models.DNN.Container> results = new();

    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      foreach (Models.DNN.Page page in await provider.ListPages(portalId))
      {
        if (!String.IsNullOrEmpty(page.ContainerSrc))
        {
          Models.DNN.Container Container = results
            .Where(existingContainer => existingContainer.ContainerSrc.Equals(page.ContainerSrc, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

          if (Container == null)
          {
            Container = new Models.DNN.Container()
            {
              ContainerSrc = page.ContainerSrc
            };

            results.Add(Container);
          }
        }
      }

      return results;
    }
  }

  private void AddPanes(Models.DNN.Skin skin, IEnumerable<string> panes)
  {
    foreach (string pane in panes)
    {
      if (!skin.Panes.Contains(pane, StringComparer.OrdinalIgnoreCase))
      {
        skin.Panes.Add(pane);
      }
    }
  }
}
