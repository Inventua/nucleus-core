using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Modules.Forums.Models;

namespace Nucleus.Modules.Forums.EventHandlers;

public class CreatePostEventHandler : Nucleus.Abstractions.EventHandlers.ISingletonSystemEventHandler<Post, Create>
{
  private ForumsManager ForumsManager { get; }
  private ILogger Logger { get; }

  public CreatePostEventHandler(ForumsManager forumsManager, ILogger<CreatePostEventHandler> logger)
  {
    this.ForumsManager = forumsManager;
    this.Logger = logger;
  }

  public async Task Invoke(Post post)
  {
    this.Logger?.LogDebug("Create post event detected, post id '{postid}'.", post.Id);

    try
    {
      // Re-get the post, as it may not be fully populated
      post = await this.ForumsManager.GetForumPost(post.Id);

      if (post.IsApproved)
      {
        await post.CreateSubscriptionEmail(this.ForumsManager, this.Logger);
      }
      else
      {
        await post.CreateModerationRequiredEmail(this.ForumsManager, this.Logger);
      }
    }
    catch (Exception ex)
    {
      this.Logger?.LogError(ex, "Create post event.");
    }
  }
}

