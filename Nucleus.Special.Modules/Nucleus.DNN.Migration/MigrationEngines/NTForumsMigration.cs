using Nucleus.Abstractions.Managers;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.FileSystemProviders;
using System.IO;
using Nucleus.DNN.Migration.Models.DNN.Modules.NTForums;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class NTForumsMigration : MigrationEngineBase<Forum>
{
  private static Guid ForumsModuleDefinitionId => new("ea9b5d66-b791-414c-8c52-a20536cfa9f5");

  private Nucleus.Abstractions.Models.Context Context { get; }
  private DNNMigrationManager MigrationManager { get; }
  private IPageModuleManager PageModuleManager { get; }
  private IUserManager UserManager { get; }
  private IFileSystemManager FileSystemManager { get; }

  public NTForumsMigration(Nucleus.Abstractions.Models.Context context, IPageModuleManager pageModuleManager, IUserManager userManager, IFileSystemManager fileSystemManager, DNNMigrationManager migrationManager) : base("Migrating Forums Content")
  {
    this.MigrationManager = migrationManager;
    this.PageModuleManager = pageModuleManager;
    this.FileSystemManager = fileSystemManager;
    this.UserManager = userManager;
    this.Context = context;
  }

  public override async Task Migrate(Boolean updateExisting)
  {
    Nucleus.Abstractions.Portable.IPortable portable = this.MigrationManager.GetPortableImplementation(ForumsModuleDefinitionId);

    FileSystemProviderInfo fileSystemProvider = this.FileSystemManager.ListProviders().FirstOrDefault();

    foreach (Forum dnnForum in this.Items)
    {
      Models.ForumInfo newForum = await this.MigrationManager.GetNucleusForumInfo(dnnForum.ForumGroup.Name, dnnForum.Name);

      if (dnnForum.CanSelect && dnnForum.IsSelected)
      {
        try
        {
          // this migration doesn't create forums (that happens during page migration), it migrates forum posts
          List<int> postIds = await this.MigrationManager.ListNTForumPostIds(dnnForum.ForumId);
          Nucleus.Abstractions.Models.PageModule newModule = await this.PageModuleManager.Get(newForum.ForumGroup.ModuleId);

          foreach (int postId in postIds)
          {
            ForumPost dnnPost = await this.MigrationManager.GetNTForumPost(postId);
            Nucleus.Abstractions.Models.User postUser = null;

            if (dnnPost.User != null)
            {
              postUser = await this.UserManager.Get(this.Context.Site, dnnPost.User.UserName);
            }

            List<int> replyIds = await this.MigrationManager.ListNTForumPostReplyIds(dnnForum.ForumId, postId);
            List<object> replies = new();

            foreach (int replyId in replyIds)
            {
              ForumPost dnnReply = await this.MigrationManager.GetNTForumPost(replyId);
              Nucleus.Abstractions.Models.User replyUser = null;

              if (dnnReply.User != null)
              {
                replyUser = await this.UserManager.Get(this.Context.Site, dnnReply.User.UserName);
              }

              List<object> replyAttachments = await BuildAttachments(newForum, fileSystemProvider, dnnReply.Attachments);

              object newReply = new
              {
                ForumId = newForum.Id,
                Body = dnnReply.Body,
                IsLocked = dnnReply.Locked,
                IsPinned = dnnReply.Pinned,
                IsApproved = dnnReply.Approved,
                IsRejected = !dnnReply.Approved,
                DateAdded = dnnReply.DateAdded,
                AddedBy = replyUser?.Id ?? Guid.Empty,
                Attachments = replyAttachments
              };

              replies.Add(newReply);
              this.Progress();
            }

            try
            {
              List<object> postAttachments = await BuildAttachments(newForum, fileSystemProvider, dnnPost.Attachments);

              object newPost = new
              {
                _type = "ForumPost",
                ForumId = newForum.Id,
                Subject = dnnPost.Subject,
                Body = dnnPost.Body,
                DateAdded = dnnPost.DateAdded,
                AddedBy = postUser?.Id ?? Guid.Empty,
                IsLocked = dnnPost.Locked,
                IsPinned = dnnPost.Pinned,
                IsApproved = dnnPost.Approved,
                IsRejected = !dnnPost.Approved,
                Replies = replies,
                Attachments = postAttachments
              };

              await portable.Import(newModule, new Nucleus.Abstractions.Portable.PortableContent("urn:nucleus:entities:forum-post", newPost));
            }
            catch (Exception ex)
            {
              dnnForum.AddError($"Error importing Forum post '{dnnPost.Subject}' for forum '{dnnForum.Name}': {ex.Message}");
            }

            this.Progress();
          }
        }
        catch (Exception ex)
        {
          dnnForum.AddError($"Error importing posts for Forum '{dnnForum.Name}': {ex.Message}");
          this.TotalCount -= dnnForum.PostCount;
        }
      }
      else
      {
        // this doesn't need a warning
        //dnnForum.AddWarning($"Forum '{dnnForum.Name}' was not selected for import.");
      }
    }

    this.SignalCompleted();
  }

  private async Task<List<object>> BuildAttachments(Models.ForumInfo newForum, FileSystemProviderInfo fileSystemProvider, List<ForumPostAttachment> dnnAttachments)
  {
    List<object> nucleusAttachments = new();

    if (dnnAttachments != null)
    {
      foreach (ForumPostAttachment dnnAttachment in dnnAttachments)
      {
        try
        {
          Nucleus.Abstractions.Models.FileSystem.File file = await this.FileSystemManager.GetFile(this.Context.Site, fileSystemProvider.Key, $"NTForums_Attach/{dnnAttachment.Filename?.Trim()}");
          if (file != null)
          {
            nucleusAttachments.Add(new { File = file });
          }
        }
        catch (FileNotFoundException)
        {
          // skip missing files
        }
      }
    }

    return nucleusAttachments;
  }

  public override async Task Validate()
  {
    foreach (Forum forum in this.Items)
    {
      if (!(await this.MigrationManager.ForumExists(forum.ForumGroup.Name, forum.Name)))
      {
        forum.AddError($"'{forum.Name}' has not been migrated, so forum posts for it can't be migrated.");
      }
    }
  }
}
