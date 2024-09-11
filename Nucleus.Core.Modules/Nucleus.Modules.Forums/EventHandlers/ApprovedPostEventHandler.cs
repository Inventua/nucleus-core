using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Modules.Forums.Models;

namespace Nucleus.Modules.Forums.EventHandlers;

public class ApprovedPostEventHandler : Nucleus.Abstractions.EventHandlers.ISingletonSystemEventHandler<Post, Approved>
{
  private ForumsManager ForumsManager { get; }
  private IUserManager UserManager { get; }
  private ILogger Logger { get; }

  public ApprovedPostEventHandler(ForumsManager forumsManager, IUserManager userManager, ILogger<ApprovedPostEventHandler> logger)
  {
    this.ForumsManager = forumsManager;
    this.UserManager = userManager;
    this.Logger = logger;
  }

  public async Task Invoke(Post post)
  {
    this.Logger?.LogDebug("Approved post event detected, post id '{postid}'.", post.Id);

    try
    {
      // Re-get the post, as it may not be fully populated
      post = await this.ForumsManager.GetForumPost(post.Id);

      if (post.IsApproved)
      {
        await post.CreateModerationApprovedEmail(this.ForumsManager, this.UserManager, this.Logger);
        await post.CreateSubscriptionEmail(this.ForumsManager, this.Logger);
      }
      else
      {
        this.Logger?.LogDebug("Skipped notification for '{postid}' because it is not approved'.", post.Id);
      }
    }
    catch (Exception ex)
    {
      this.Logger?.LogError(ex, "Approved post event.");
    }
  }

}

