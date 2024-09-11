using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Modules.Forums.Models;

namespace Nucleus.Modules.Forums.EventHandlers;

public class CreateReplyEventHandler : Nucleus.Abstractions.EventHandlers.ISingletonSystemEventHandler<Reply, Create>
{
  private ForumsManager ForumsManager { get; }
  private ILogger Logger { get; }

  public CreateReplyEventHandler(ForumsManager forumsManager, ILogger<CreateReplyEventHandler> logger)
  {
    this.ForumsManager = forumsManager;
    this.Logger = logger;
  }

  public async Task Invoke(Reply reply)
  {
    this.Logger?.LogDebug("Create reply event detected, reply id '{replyid}'.", reply.Id);

    try
    {
      // Re-get the reply, as it may not be fully populated
      reply = await this.ForumsManager.GetForumPostReply(reply.Id);

      if (reply.IsApproved)
      {
        await reply.CreateSubscriptionEmail(this.ForumsManager, this.Logger);
      }
      else
      {
        await reply.CreateModerationRequiredEmail(this.ForumsManager, this.Logger);
      }
    }
    catch (Exception ex)
    {
      this.Logger?.LogError(ex, "Create reply event.");
    }
  }
}

