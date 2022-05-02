using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Modules.Forums.Models;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;

namespace Nucleus.Modules.Forums.EventHandlers
{
	public class CreateReplyEventHandler : Nucleus.Abstractions.EventHandlers.ISystemEventHandler<Reply, Create>
	{		
		private ForumsManager ForumsManager { get; }

		public CreateReplyEventHandler(ForumsManager forumsManager)
		{
			this.ForumsManager = forumsManager;
		}

		public async Task Invoke(Reply reply)
		{
			// Re-get the reply, as it may not be fully populated
			reply = await this.ForumsManager.GetForumPostReply(reply.Id);

			if (reply.IsApproved)
			{
				await reply.CreateSubscriptionEmail(this.ForumsManager);
			}
			else
			{
				await reply.CreateModerationRequiredEmail(this.ForumsManager);
			}

		}
	}
}

