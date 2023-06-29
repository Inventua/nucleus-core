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
	public class RejectedPostEventHandler : Nucleus.Abstractions.EventHandlers.ISingletonSystemEventHandler<Post, Rejected>
	{		
		private ForumsManager ForumsManager { get; }
		private IUserManager UserManager { get; }

		public RejectedPostEventHandler(ForumsManager forumsManager, IUserManager userManager)
		{
			this.ForumsManager = forumsManager;
			this.UserManager = userManager;
		}

		public async Task Invoke(Post post)
		{
			// Re-get the post, as it may not be fully populated
			post = await this.ForumsManager.GetForumPost(post.Id);

			if (post.IsRejected == true)
			{
				await post.CreateModerationRejectedEmail(this.ForumsManager, this.UserManager);
			}
		}

	}
}

