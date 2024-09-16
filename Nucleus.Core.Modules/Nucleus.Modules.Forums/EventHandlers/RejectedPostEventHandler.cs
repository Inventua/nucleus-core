using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Modules.Forums.Models;

namespace Nucleus.Modules.Forums.EventHandlers;

public class RejectedPostEventHandler : Nucleus.Abstractions.EventHandlers.ISingletonSystemEventHandler<Post, Rejected>
{
  private ForumsManager ForumsManager { get; }
  private IUserManager UserManager { get; }
  private ILogger Logger { get; }

  public RejectedPostEventHandler(ForumsManager forumsManager, IUserManager userManager, ILogger<RejectedPostEventHandler> logger)
  {
    this.ForumsManager = forumsManager;
    this.UserManager = userManager;
    this.Logger = logger;
  }

  public async Task Invoke(Post post)
  {
    this.Logger?.LogDebug("Rejected post event detected, post id '{postid}'.", post.Id);

    try
    {
      // Re-get the post, as it may not be fully populated
      post = await this.ForumsManager.GetForumPost(post.Id);

      if (post.IsRejected == true)
      {
        await post.CreateModerationRejectedEmail(this.ForumsManager, this.UserManager, this.Logger);
      }
    }
    catch (Exception ex)
    {
      this.Logger?.LogError(ex, "Rejected post event.");
    }
  }
}

