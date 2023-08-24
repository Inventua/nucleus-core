using Nucleus.Abstractions.Managers;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.FileSystemProviders;
using System.IO;
using Nucleus.DNN.Migration.Models.DNN.Modules.ActiveForums;
using System.Net;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class ActiveForumsMigration : MigrationEngineBase<Forum>
{
  private static Guid ForumsModuleDefinitionId => new("ea9b5d66-b791-414c-8c52-a20536cfa9f5");

  private Nucleus.Abstractions.Models.Context Context { get; }
  private DNNMigrationManager MigrationManager { get; }
  private IPageModuleManager PageModuleManager { get; }
  private IUserManager UserManager { get; }
  private IFileSystemManager FileSystemManager { get; }

  public ActiveForumsMigration(Nucleus.Abstractions.Models.Context context, IPageModuleManager pageModuleManager, IUserManager userManager, IFileSystemManager fileSystemManager, DNNMigrationManager migrationManager) : base("Migrating Forums Content")
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
          List<int> topicIds = await this.MigrationManager.ListActiveForumsTopicIds(dnnForum.ForumId);
          Nucleus.Abstractions.Models.PageModule newModule = await this.PageModuleManager.Get(newForum.ForumGroup.ModuleId);

          foreach (int postId in topicIds)
          {
            ForumTopic dnnTopic = await this.MigrationManager.GetActiveForumsTopic(postId);
            Nucleus.Abstractions.Models.User postUser = null;

            if (dnnTopic.Content?.User != null)
            {
              postUser = await this.UserManager.Get(this.Context.Site, dnnTopic.Content?.User.UserName);
            }

            List<int> replyIds = await this.MigrationManager.ListActiveForumsReplyIds(dnnForum.ForumId, postId);
            List<object> replies = new();

            foreach (int replyId in replyIds)
            {
              ForumReply dnnReply = await this.MigrationManager.GetActiveForumsReply(replyId);
              Nucleus.Abstractions.Models.User replyUser = null;

              if (dnnReply.Content?.User != null)
              {
                replyUser = await this.UserManager.Get(this.Context.Site, dnnReply.Content?.User.UserName);
              }

              List<object> replyAttachments = await BuildAttachments(newForum, fileSystemProvider, dnnReply.Content?.Attachments);

              object newReply = new
              {
                ForumId = newForum.Id,
                Body = ParseForumPostBody(dnnReply.Content?.Body ?? ""),
                IsLocked = false, //dnnReply.Locked,
                IsPinned = false, // dnnReply.Pinned,
                IsApproved = dnnReply.IsApproved,
                IsRejected = dnnReply.IsRejected,
                DateAdded = dnnReply.Content?.DateAdded ?? DateTime.Now,
                AddedBy = replyUser?.Id ?? Guid.Empty,
                Attachments = replyAttachments
              };

              replies.Add(newReply);
            }

            try
            {
              List<object> postAttachments = await BuildAttachments(newForum, fileSystemProvider, dnnTopic.Content?.Attachments);

              object newPost = new
              {
                _type = "ForumPost",
                ForumId = newForum.Id,
                Subject = dnnTopic.Content?.Subject ?? "",
                Body = ParseForumPostBody(dnnTopic.Content?.Body ?? ""),
                DateAdded = dnnTopic.Content?.DateAdded ?? DateTime.UtcNow,
                AddedBy = postUser?.Id ?? Guid.Empty,
                IsLocked = dnnTopic.IsLocked,
                IsPinned = dnnTopic.IsPinned,
                IsApproved = dnnTopic.IsApproved,
                IsRejected = dnnTopic.IsRejected,
                Replies = replies,
                Attachments = postAttachments
              };

              await portable.Import(newModule, new Nucleus.Abstractions.Portable.PortableContent("urn:nucleus:entities:forum-post", newPost));
            }
            catch (Exception ex)
            {
              dnnForum.AddError($"Error importing Forum post '{dnnTopic.Content?.Subject}' for forum '{dnnForum.Name}': {ex.Message}");
            }

            this.Progress();
          }
        }
        catch (Exception ex)
        {
          dnnForum.AddError($"Error importing posts for Forum '{dnnForum.Name}': {ex.Message}");
          this.Progress(dnnForum.PostCount);
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

  private string ParseForumPostBody(string value)
  {
    value = WebUtility.HtmlDecode(value);

    foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(value, "(?<element>\\[(?<closetag>[\\/]*)(?<elementname>.*?)\\])"))
    {
      if (match.Success && match.Groups.ContainsKey("elementname"))
      {
        switch (match.Groups["elementname"].Value)
        {
          case "quote":
            value = value.Replace(match.Value, $"<{match.Groups["closetag"].Value}blockquote>");
            break;
        }
      }
    }

    return value;
  }

  private async Task<List<object>> BuildAttachments(Models.ForumInfo newForum, FileSystemProviderInfo fileSystemProvider, List<ForumAttachment> dnnAttachments)
  {
    List<object> nucleusAttachments = new();

    if (dnnAttachments != null)
    {
      foreach (ForumAttachment dnnAttachment in dnnAttachments)
      {
        try
        {
          Nucleus.Abstractions.Models.FileSystem.File file = await this.FileSystemManager.GetFile(this.Context.Site, fileSystemProvider.Key, $"ActiveForums_Attach/{dnnAttachment.Filename?.Trim()}");
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
