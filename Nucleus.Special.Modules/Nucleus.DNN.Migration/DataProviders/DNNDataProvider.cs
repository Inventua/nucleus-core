using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models;
using Nucleus.DNN.Migration.Models.DNN;
using Nucleus.DNN.Migration.Models.DNN.Modules.NTForums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Nucleus.DNN.Migration.DataProviders;

public class DNNDataProvider : Nucleus.Data.EntityFramework.DataProvider
{
  protected new DNNDbContext Context { get; }

  public DNNDataProvider(DNNDbContext context, ILogger<DNNMigrationDataProvider> logger) : base(context, logger)
  {
    this.Context = context;
  }

  public async Task<Models.DNN.Version> GetVersion()
  {
    return await this.Context.Version
      .OrderByDescending(version => version.Major)
        .ThenByDescending(version => version.Minor)
        .ThenByDescending(version => version.Build)
      .AsNoTracking()
      .FirstOrDefaultAsync();
  }

  public async Task<List<Models.DNN.Portal>> ListPortals()
  {
    return await this.Context.Portals
      .Include(portal => portal.PortalAliases)
      .AsNoTracking()
      .ToListAsync();
  }

  public async Task<Models.DNN.RoleGroup> GetRoleGroup(int roleGroupId)
  {
    return await this.Context.RoleGroups
      .Where(group => group.RoleGroupId == roleGroupId)
      .AsNoTracking()
      .FirstOrDefaultAsync();
  }

  public async Task<List<Models.DNN.RoleGroup>> ListRoleGroups(int portalId)
  {
    List<Models.DNN.RoleGroup> results = await this.Context.RoleGroups
      .Where(group => group.PortalId == portalId && group.Roles.Any())
      .OrderBy(group => group.RoleGroupName)
      .AsNoTracking()
      .ToListAsync();

    foreach (var roleGroup in results)
    {
      roleGroup.RoleCount = await this.Context.Roles
        .Where(role => role.RoleGroup.RoleGroupId == roleGroup.RoleGroupId)
        .AsNoTracking()
        .CountAsync();
    }

    return results;
  }

  public async Task<Models.DNN.Role> GetRole(int roleId)
  {
    return await this.Context.Roles
      .Where(role => role.RoleId == roleId)
      .Include(role => role.RoleGroup)
      .AsNoTracking()
      .FirstOrDefaultAsync();
  }

  public async Task<List<Models.DNN.Role>> ListRoles(int portalId)
  {
    List<Models.DNN.Role> results = await this.Context.Roles
      .Where(role => role.PortalId == portalId)
      .OrderBy(role => role.RoleName)
      .Include(role => role.RoleGroup)
      .AsNoTracking()
      .ToListAsync();

    foreach (var role in results) 
    {
      role.UserCount = await this.Context.UserRoles
        .Where(userRole => userRole.RoleId == role.RoleId)
        .AsNoTracking()
        .CountAsync();
    }

    return results;
  }

  public async Task<List<Models.DNN.List>> ListLists(int portalId)
  {
    List<Models.DNN.List> results = await this.Context.ListItems
      .Where(item => item.PortalId ==-1 || item.PortalId == portalId)
      .GroupBy(listitem => listitem.ListName)
      .Select(group => new Models.DNN.List() 
      { 
        ListName = group.Key, 
        PortalId = portalId, 
        SystemList = !group.Any() ? false : group.First().SystemList 
      })
      .AsNoTracking()
      .ToListAsync();

    foreach (Models.DNN.List list in results)
    {
      list.ListItems = await this.Context.ListItems
        .Where(item => item.ListName == list.ListName)
        .OrderBy(item => item.SortOrder)
        .AsNoTracking()
        .ToListAsync();
    }

    return results;
  }


  public async Task<Models.DNN.File> GetFile(int fileId)
  {
    return await this.Context.Files
      .Where(file => file.FileId == fileId)
      .Include(file => file.Folder)
      .AsNoTracking()
      .FirstOrDefaultAsync();
  }


  public async Task<Models.DNN.User> GetUser(int userId)
  {
    return await this.Context.Users
      .Where(user => user.UserId == userId)
      .Include(user => user.Roles)
      .Include(user => user.ProfileProperties)
      .AsSplitQuery()
      .AsNoTracking()
      .FirstOrDefaultAsync();
  }

  public async Task<List<Models.DNN.User>> ListUsers(int portalId)
  {
    List<Models.DNN.User> results = await this.Context.Users
      .Where(user => user.UserPortals.Where(portal => portal.Portal.PortalId == portalId).Any())
      .Include(user => user.Roles)
      .Include(user => user.ProfileProperties)
      .Include(user => user.UserPortals)
      .OrderBy(user => user.UserName)
      .AsSplitQuery()
      .AsNoTracking()
      .ToListAsync();

    List<Models.DNN.UserProfilePropertyDefinition> propertyDefinitions = await this.Context.UserProfilePropertyDefinitions
      .AsNoTracking()
      .ToListAsync();

    foreach (Models.DNN.User user in results)
    {
      user.UserPortal = user.UserPortals
        .Where(userPortal => userPortal.UserId == user.UserId && userPortal.PortalId == portalId)
        .FirstOrDefault();

      //user.UserPortal = await this.Context.UserPortals
      //  .Where(userPortal => userPortal.UserId == user.UserId && userPortal.Portal.PortalId == portalId)
      //  .AsNoTracking()
      //  .FirstOrDefaultAsync();

      foreach (Models.DNN.UserProfileProperty prop in user.ProfileProperties)
      {
        prop.PropertyDefinition = propertyDefinitions
          .Where(def => def.PropertyDefinitionId == prop.PropertyDefinitionId)
          .FirstOrDefault();
        //prop.PropertyDefinition = this.Context.UserProfilePropertyDefinitions
        //  .Where(def => def.PropertyDefinitionId == prop.PropertyDefinitionId)
        //  .FirstOrDefault();
      }
    }
    return results;
  }

  public async Task<Models.DNN.Page> GetPage(int pageId)
  {
    return await this.Context.Pages
      .Where(page => page.PageId == pageId)
      .Include(page => page.PageModules)
        .ThenInclude(module => module.DesktopModule)
      .AsNoTracking()
      .AsSplitQuery()
      .FirstOrDefaultAsync();
  }

  public async Task<List<Models.DNN.Page>> ListPages(int portalId)
  {
    List<Models.DNN.Page> results = await this.Context.Pages
      .Where(page => 
        page.PortalId == portalId && 
        page.PageName != "Admin" && !page.TabPath.StartsWith("//Admin") && // exclude "Admin" page, and descendants
        !page.IsDeleted 
      )
      .Include(page => page.Permissions)
      .Include(page => page.PageModules)
        .ThenInclude(pageModule => pageModule.Settings)
      .Include(page => page.PageModules) 
        .ThenInclude(module => module.DesktopModule)
      .Include(page => page.PageModules)
        .ThenInclude(module => module.Permissions)
      .OrderBy(page => page.Level)
        .ThenBy(page => page.ParentId)
        .ThenBy(page => page.PageId)
        .ThenBy(page => page.PageName)
      .AsSplitQuery()
      .AsNoTracking()
      .ToListAsync();

    ////foreach (Models.DNN.Page page in results)
    ////{
    ////  foreach (Models.DNN.PageModule module in page.PageModules)
    ////  {
    ////    module.Settings = await this.Context.PageModuleSettings
    ////      .Where(setting => setting.ModuleId == module.ModuleId)
    ////      .AsNoTracking()
    ////      .ToListAsync();
    ////  }
    ////}
    return results;
  }

  public async Task<List<Models.DNN.PageUrl>> ListPageUrls(int pageId)
  {
    return await this.Context.PageUrls
      .Where(pageUrl => pageUrl.PageId == pageId)
      .OrderBy(pageUrl => pageUrl.SeqNum)
      .AsNoTracking()
      .ToListAsync();
  }

  public async Task<Models.DNN.Modules.TextHtml> GetHtmlContent(int moduleId)
  {
    return await this.Context.TextHtml
      .Where(textHtml => textHtml.ModuleId == moduleId && textHtml.IsPublished)
      .OrderByDescending(textHtml => textHtml.Version)
      .AsNoTracking()
      .FirstOrDefaultAsync();
  }

  public async Task<Models.DNN.Modules.DocumentsSettings> GetDocumentsSettings(int moduleId)
  {
    return await this.Context.DocumentsSettings
      .Where(settings => settings.ModuleId == moduleId)
      .AsNoTracking()
      .FirstOrDefaultAsync();
  }

  public async Task<Models.DNN.Modules.MediaSettings> GetMediaSettings(int moduleId)
  {
    return await this.Context.MediaSettings
      .Where(settings => settings.ModuleId == moduleId)
      .AsNoTracking()
      .FirstOrDefaultAsync();
  }


  public async Task<List<Models.DNN.Modules.Document>> ListDocuments(int moduleId)
  {
    return await this.Context.Documents
      .Where(document => document.ModuleId == moduleId)
      .AsNoTracking()
      .ToListAsync();
  }


  public async Task<List<Models.DNN.Modules.Link>> ListLinks(int moduleId)
  {
    return await this.Context.Links
      .Where(link => link.ModuleId == moduleId)
      .AsNoTracking()
      .ToListAsync();
  }

  public async Task<List<Models.DNN.Modules.Blog>> ListBlogs(int portalId)
  {
    return await this.Context.Blogs
      .Where(blog => blog.PortalId == portalId && blog.Public)
      .Include(blog => blog.BlogEntries)
      .AsNoTracking()
      .ToListAsync();
  }

  public async Task<Models.DNN.Folder> GetFolder(int folderId)
  {
    return await this.Context.Folders
      .Where(folder => folder.FolderId == folderId)
      .Include(folder => folder.Files)
      .Include(folder => folder.Permissions)
      .AsSplitQuery()
      .AsNoTracking()
      .FirstOrDefaultAsync();
  }

  public async Task<List<Models.DNN.Folder>> ListFolders(int portalId)
  {    
    return await this.Context.Folders
      .Where(folder => folder.PortalId == portalId)
      .Include(folder => folder.Files)
      .OrderBy(folder => folder.FolderPath)
      .AsNoTracking()
      .ToListAsync();
  }

  public async Task<List<ForumGroup>> ListNTForumGroupsByModule(int moduleId)
  {
    return await this.Context.NTForumsGroups
      .Where(group => group.ModuleId == moduleId)
      .Include(group => group.Forums)
      .Include(group => group.Settings)
      .AsNoTracking()
      .ToListAsync();
  }

  public async Task<List<ForumGroup>> ListNTForumGroupsByPortal(int portalId)
  {
    return await this.Context.NTForumsGroups
      .Where(group => group.PortalId == portalId)
      .Include(group => group.Forums)
      .Include(group => group.Settings)
      .AsNoTracking()
      .ToListAsync();
  }

  public async Task<int> CountNTForumPosts(int forumId)
  {
    return await this.Context.NTForumsPosts
      .Where(post => post.ForumId == forumId && post.Deleted == false && post.Archived == false)
      .OrderBy(post => post.ParentPostId)
      .AsNoTracking()
      .CountAsync();
  }

  public async Task<List<int>> ListNTForumPostIds(int forumId)
  {
    return await this.Context.NTForumsPosts
      .Where(post => post.ForumId == forumId && post.ParentPostId == 0 && post.Deleted == false && post.Archived == false)
      .OrderBy(post => post.ParentPostId)
      .Select(post => post.PostId)
      .ToListAsync();
  }

  public async Task<List<int>> ListNTForumPostReplyIds(int forumId, int postId)
  {
    return await this.Context.NTForumsPosts
      .Where(post => post.ForumId == forumId && post.ParentPostId == postId && post.Deleted == false && post.Archived == false)
      .Select(post => post.PostId)
      .ToListAsync();
  }

  public async Task<ForumPost> GetNTForumPost(int postId)
  {
    return await this.Context.NTForumsPosts
      .Where(post => post.PostId == postId)
      .Include(post => post.User)
      .Include(post => post.Attachments)
      .AsSplitQuery()
      .FirstOrDefaultAsync();    
  }


  //

  public async Task<List<Models.DNN.Modules.ActiveForums.ForumGroup>> ListActiveForumsGroupsByModule(int moduleId)
  {
    return await this.Context.ActiveForumsGroups
      .Where(group => group.ModuleId == moduleId)
      .Include(group => group.Permissions)
      .Include(group => group.Forums)
        .ThenInclude(forum => forum.Permissions)
      .AsNoTracking()
      .AsSplitQuery()
      .ToListAsync();
  }

  public async Task<List<Models.DNN.Modules.ActiveForums.ForumSetting>> ListActiveForumsSettings(int moduleId, string key)
  {
    return await this.Context.ActiveForumsSettings
      .Where(setting => setting.ModuleId == moduleId && setting.GroupKey == key)
      .AsNoTracking()
      .ToListAsync();
  }

  public async Task<List<Models.DNN.Modules.ActiveForums.ForumGroup>> ListActiveForumsGroupsByPortal(int portalId)
  {
    return await this.Context.ActiveForumsGroups
      .Where(group => group.Forums.Where(forum => forum.PortalId == portalId).Any())
      .Include(group => group.Permissions)
      .Include(group => group.Forums)
        .ThenInclude(forum => forum.Permissions)
      .AsSplitQuery()
      .AsNoTracking()
      .ToListAsync();
  }

  public async Task<int> CountActiveForumsTopics(int forumId)
  {
    return await this.Context.ActiveForumsTopics
      .Where(topic => topic.IsDeleted == false && topic.IsArchived == false && topic.TopicLinks.Where(link => link.Forum.ForumId == forumId).Any())
      .AsNoTracking()
      .CountAsync();
  }

  public async Task<List<int>> ListActiveForumsTopicIds(int forumId)
  {
    return await this.Context.ActiveForumsTopics
      .Where(topic => topic.IsDeleted == false && topic.IsArchived == false && topic.TopicLinks.Where(link => link.Forum.ForumId == forumId).Any())
      .OrderBy(topic => topic.TopicId)
      .Select(topic => topic.TopicId)
      .ToListAsync();
  }

  public async Task<List<int>> ListActiveForumsReplyIds(int forumId, int topicId)
  {
    return await this.Context.ActiveForumsReplies
      .Where(reply => reply.IsDeleted == false && reply.IsRejected == false && reply.Topic.TopicLinks.Where(link => link.ForumId == forumId).Any() && reply.Topic.TopicId == topicId)
      .Select(reply => reply.ReplyId)
      .ToListAsync();
  }

  public async Task<Models.DNN.Modules.ActiveForums.ForumTopic> GetActiveForumsTopic(int topicId)
  {
    return await this.Context.ActiveForumsTopics
      .Where(topic => topic.TopicId == topicId)      
      .Include(topic => topic.Content)
        .ThenInclude(content => content.Attachments)
      .Include(topic => topic.Content)
        .ThenInclude(content => content.User)
      .AsSplitQuery()
      .FirstOrDefaultAsync();
  }

  public async Task<Models.DNN.Modules.ActiveForums.ForumReply> GetActiveForumsReply(int replyId)
  {
    return await this.Context.ActiveForumsReplies
      .Where(reply => reply.ReplyId == replyId)
      .Include(reply => reply.Content)
        .ThenInclude(content => content.Attachments)
      .Include(topic => topic.Content)
        .ThenInclude(content => content.User)
      .AsSplitQuery()
      .FirstOrDefaultAsync();
  }
}
