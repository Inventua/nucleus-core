using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Modules.Forums.Models;

namespace Nucleus.Modules.Forums.EventHandlers;

public class ApprovedReplyEventHandler : Nucleus.Abstractions.EventHandlers.ISingletonSystemEventHandler<Reply, Approved>
	{		
		private ForumsManager ForumsManager { get; }
		private IUserManager UserManager { get; }
  private ILogger Logger { get; }

  public ApprovedReplyEventHandler(ForumsManager forumsManager, IUserManager userManager, ILogger<ApprovedReplyEventHandler> logger)
		{
  	this.ForumsManager = forumsManager;
			this.UserManager = userManager;
    this.Logger = logger;
		}

  public async Task Invoke(Reply reply)
  {
    this.Logger?.LogDebug("Approve reply event detected, reply id '{replyid}'.", reply.Id);
    try
    {
      // Re-get the reply, as it may not be fully populated
      reply = await this.ForumsManager.GetForumPostReply(reply.Id);

      if (reply.IsApproved)
      {
        await reply.CreateModerationApprovedEmail(this.ForumsManager, this.UserManager, this.Logger);
        await reply.CreateSubscriptionEmail(this.ForumsManager, this.Logger);
      }
      else
      {
        this.Logger?.LogDebug("Skipped notification for '{replyid}' because it is not approved'.", reply.Id);
      }
    }
    catch (Exception ex)
    {
      this.Logger?.LogError(ex, "Approve reply event.");
    }
  }
}

