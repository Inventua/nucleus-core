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
	public class ApprovedPostEventHandler : Nucleus.Abstractions.EventHandlers.ISingletonSystemEventHandler<Post, Approved>
	{		
		private ForumsManager ForumsManager { get; }
		private IUserManager UserManager { get; }

		public ApprovedPostEventHandler(ForumsManager forumsManager, IUserManager userManager)
		{
			this.ForumsManager = forumsManager;
			this.UserManager = userManager;
		}

		public async Task Invoke(Post post)
		{
			// Re-get the post, as it may not be fully populated
			post = await this.ForumsManager.GetForumPost(post.Id);

			if (post.IsApproved)
			{
				await post.CreateModerationApprovedEmail(this.ForumsManager, this.UserManager);
				await post.CreateSubscriptionEmail(this.ForumsManager);
			}
		}

	}
}

