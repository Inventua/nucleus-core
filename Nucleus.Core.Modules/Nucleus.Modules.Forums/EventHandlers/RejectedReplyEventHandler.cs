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
	public class RejectedReplyEventHandler : Nucleus.Abstractions.EventHandlers.ISystemEventHandler<Reply, Rejected>
	{		
		private ForumsManager ForumsManager { get; }
		private IUserManager UserManager { get; }

		public RejectedReplyEventHandler(ForumsManager forumsManager, IUserManager userManager)
		{
			this.ForumsManager = forumsManager;
			this.UserManager = userManager;
		}

		public async Task Invoke(Reply reply)
		{
			// Re-get the reply, as it may not be fully populated
			reply = await this.ForumsManager.GetForumPostReply(reply.Id);

			if (!reply.IsApproved)
			{
				await reply.CreateModerationRejectedEmail(this.ForumsManager, this.UserManager);
			}
		}
	}
}

