using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nucleus.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Portable;
using Nucleus.Abstractions.Managers;
using System.Security.Claims;
using Nucleus.Modules.Forums.Models;

namespace Nucleus.Modules.Forums;

public class Portable : Nucleus.Abstractions.Portable.IPortable
{
  private ISiteManager SiteManager { get; }
  private IPageManager PageManager { get; }
  private GroupsManager GroupsManager { get; }
  private ForumsManager ForumsManager { get; }
  public IUserManager UserManager { get; }

  public Portable(ISiteManager siteManager, IUserManager userManager, IPageManager pageManager, GroupsManager groupsManager, ForumsManager forumsManager)
  {
    this.SiteManager = siteManager;
    this.PageManager = pageManager;
    this.UserManager = userManager;
    this.GroupsManager = groupsManager;
    this.ForumsManager = forumsManager;
  }

  public Guid ModuleDefinitionId => new Guid("ea9b5d66-b791-414c-8c52-a20536cfa9f5");

  public string Name => "Forums";

  

  public Task<List<object>> Export(PageModule module)
  {
    throw new NotImplementedException();
  }

  public async Task Import(PageModule module, List<object> items)
  {
    Abstractions.Models.Page page = await this.PageManager.Get(module);
    Abstractions.Models.Site site = await this.SiteManager.Get(page);

    foreach (object item in items)
    {
      ImportItem import = item.CopyTo<ImportItem>();
      switch (import._type)
      {
        case "ForumPost":
          await ImportPost(module, item.CopyTo<Models.Post>());
          break;

        default:
          await ImportGroup(module, item.CopyTo<Models.Group>());
          break;
      }
      
    }
  }

  private class ImportItem
  {
    public string _type { get; set; }
  }

  private async Task ImportGroup(PageModule module, Models.Group group)
  {
    Models.Group existingGroup = (await this.GroupsManager.List(module))
            .Where(existing => existing.Name.Equals(group.Name, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

    if (existingGroup != null)
    {
      group.Id = existingGroup.Id;
    }

    await this.GroupsManager.Save(module, group);
    
    if (group.Forums != null)
    {
      foreach (Models.Forum forum in group.Forums)
      {
        Models.Forum existingForum = (await this.ForumsManager.List(group))
          .Where(existing => existing.Name.Equals(forum.Name, StringComparison.OrdinalIgnoreCase))
          .FirstOrDefault();

        if (existingForum != null)
        {
          forum.Id = existingForum.Id;
        }

        await this.ForumsManager.Save(group, forum);
      }
    }
  }

  private async Task ImportPost(PageModule module, Models.Post post)
  {
    Nucleus.Abstractions.Models.Page page = await this.PageManager.Get(module.PageId);
    Nucleus.Abstractions.Models.Site site = await this.SiteManager.Get(page.SiteId);

    Models.Forum forum = await this.ForumsManager.Get(post.ForumId);

    Models.Post existingPost = await this.ForumsManager.FindForumPost(post.ForumId, post.Subject);

    if (existingPost != null)
    {
      existingPost.Subject = post.Subject;
      existingPost.Body = post.Body;
      existingPost.IsRejected = post.IsRejected;
      existingPost.IsApproved = post.IsApproved;
      existingPost.IsLocked= post.IsLocked;
      existingPost.IsPinned= post.IsPinned;
      existingPost.DateAdded = post.DateAdded;
      existingPost.DateChanged = post.DateChanged;

      if (existingPost.Attachments == null || !existingPost.Attachments.Any())
      {
        existingPost.Attachments = post.Attachments;
      }

      await this.ForumsManager.SavePost(site, null, forum, existingPost);
      post.Id = existingPost.Id;
    }
    else
    {
      await this.ForumsManager.SavePost(site, null, forum, post);
    }  

    if (post.Replies != null)
    {
      if (post.Replies.Count > 1)
      {

      }
      foreach (Models.Reply reply in post.Replies)
      {
        Models.Reply existingReply = (await this.ForumsManager.ListPostReplies(site, post, Models.FlagStates.IsAny))
          .Where(existingReply => existingReply.DateAdded == reply.DateAdded)
          .FirstOrDefault();

        if (existingReply != null)
        {
          existingReply.Body = reply.Body;
          existingReply.IsRejected = reply.IsRejected;
          existingReply.IsApproved = reply.IsApproved;
          existingReply.IsRejected = post.IsRejected;
          existingReply.IsApproved = post.IsApproved;
          existingReply.DateAdded = post.DateAdded;
          existingReply.DateChanged = post.DateChanged;

          if (existingReply.Attachments == null || !existingReply.Attachments.Any())
          {
            existingReply.Attachments = reply.Attachments;
          }

          await this.ForumsManager.SavePostReply(site, null, post, existingReply);
        }
        else
        {
          await this.ForumsManager.SavePostReply(site, null, post, reply);
        }
      }
    }
  }
}
