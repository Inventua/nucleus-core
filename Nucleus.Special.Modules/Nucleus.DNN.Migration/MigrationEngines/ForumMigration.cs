using DocumentFormat.OpenXml.EMMA;
using Nucleus.Abstractions.Managers;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class ForumMigration : MigrationEngineBase<Models.DNN.Modules.Forum>
{
  private static Guid ForumsModuleDefinitionId => new("ea9b5d66-b791-414c-8c52-a20536cfa9f5");

  private Nucleus.Abstractions.Models.Context Context { get; }
  private DNNMigrationManager MigrationManager { get; }
  private IPageModuleManager PageModuleManager { get; }
  private IUserManager UserManager { get; }

  public ForumMigration(Nucleus.Abstractions.Models.Context context, IPageModuleManager pageModuleManager, IUserManager userManager, DNNMigrationManager migrationManager) : base("Forums")
  {
    this.MigrationManager = migrationManager;
    this.PageModuleManager = pageModuleManager;
    this.UserManager = userManager;
    this.Context = context;
  }

  public override async Task Migrate(Boolean updateExisting)
  {
    Nucleus.Abstractions.Portable.IPortable portable = this.MigrationManager.GetPortableImplementation(ForumsModuleDefinitionId);



    //this.Message = "";
    foreach (Models.DNN.Modules.Forum dnnForum in this.Items)
    {
      Models.ForumInfo newForum = await this.MigrationManager.GetNucleusForumInfo(dnnForum.ForumGroup.Name, dnnForum.Name);

      if (dnnForum.CanSelect && dnnForum.IsSelected)
      {
        try
        {
          // this migration doesn't create forums (that happens during page migration), it migrates forum posts
          List<int> postIds = await this.MigrationManager.ListForumPostIds(dnnForum.ForumId);
          Nucleus.Abstractions.Models.PageModule newModule = await this.PageModuleManager.Get(newForum.ForumGroup.ModuleId);

          foreach (int postId in postIds)
          {
            Models.DNN.Modules.ForumPost post = await this.MigrationManager.GetForumPost(postId);
            Nucleus.Abstractions.Models.User postUser = null;

            if (post.User != null)
            {
              postUser = await this.UserManager.Get(this.Context.Site, post.User.UserName);
            }

            List<int> replyIds = await this.MigrationManager.ListForumPostReplyIds(dnnForum.ForumId, postId);
            List<object> replies = new();

            foreach (int replyId in replyIds)
            {
              Models.DNN.Modules.ForumPost reply = await this.MigrationManager.GetForumPost(replyId);
              Nucleus.Abstractions.Models.User replyUser = null;

              if (reply.User != null) 
              { 
                replyUser = await this.UserManager.Get(this.Context.Site, reply.User.UserName);
              }

              //todo
              List<object> replyAttachments = new();

              object newReply = new
              {
                ForumId = newForum.Id,
                Body = reply.Body,
                IsLocked = reply.Locked,
                IsPinned = reply.Pinned,
                IsApproved = reply.Approved,
                IsRejected = !reply.Approved,
                DateAdded = reply.DateAdded,
                AddedBy = replyUser?.Id ?? Guid.Empty,
                Attachments = replyAttachments
              };

              replies.Add(newReply);
            }

            try
            {
              //todo
              List<object> postAttachments = new();

              object newPost = new
              {
                _type = "ForumPost",
                ForumId = newForum.Id,
                Subject = post.Subject,
                Body = post.Body,
                DateAdded = post.DateAdded,
                AddedBy = postUser?.Id ?? Guid.Empty,
                IsLocked = post.Locked,
                IsPinned = post.Pinned,
                IsApproved = post.Approved,
                IsRejected = !post.Approved,
                Replies = replies,
                Attachments = postAttachments
              };
              // Permissions 

              await portable.Import(newModule, new List<object> { newPost });

              this.Progress();
            }
            catch (Exception ex)
            {
              dnnForum.AddError($"Error importing Forum post '{post.Subject}' for forum '{dnnForum.Name}': {ex.Message}");
            }
          }
        }
        catch (Exception ex)
        {
          dnnForum.AddError($"Error importing posts for Forum '{dnnForum.Name}': {ex.Message}");
        }

      }
      else
      {
        dnnForum.AddWarning($"Forum '{dnnForum.Name}' was not selected for import.");
      }
    }
  }

  public override async Task Validate()
  {
    foreach (Models.DNN.Modules.Forum forum in this.Items)
    {
      if (!(await this.MigrationManager.ForumExists(forum.ForumGroup.Name, forum.Name)))
      {
        forum.AddError($"'{forum.Name}' has not been migrated, so forum posts for it can't be migrated.");
      }
    }
  }
}
