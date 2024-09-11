using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Modules.Forums.Models;

namespace Nucleus.Modules.Forums.EventHandlers;

public class RejectedReplyEventHandler : Nucleus.Abstractions.EventHandlers.ISingletonSystemEventHandler<Reply, Rejected>
{
  private ForumsManager ForumsManager { get; }
  private IUserManager UserManager { get; }
  private ILogger Logger { get; }

  public RejectedReplyEventHandler(ForumsManager forumsManager, IUserManager userManager, ILogger<RejectedReplyEventHandler> logger)
  {
    this.ForumsManager = forumsManager;
    this.UserManager = userManager;
    this.Logger = logger;
  }

  public async Task Invoke(Reply reply)
  {
    this.Logger?.LogDebug("Rejected reply event detected, reply id '{replyid}'.", reply.Id);

    try
    {
      // Re-get the reply, as it may not be fully populated
      reply = await this.ForumsManager.GetForumPostReply(reply.Id);

      if (!reply.IsApproved)
      {
        await reply.CreateModerationRejectedEmail(this.ForumsManager, this.UserManager, this.Logger);
      }
    }
    catch (Exception ex)
    {
      this.Logger?.LogError(ex, "Rejected reply event.");
    }
  }
}

