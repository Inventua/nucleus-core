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
	public class CreatePostEventHandler : Nucleus.Abstractions.EventHandlers.ISystemEventHandler<Post, Create>
	{		
		private ForumsManager ForumsManager { get; }

		public CreatePostEventHandler(ForumsManager forumsManager)
		{
			this.ForumsManager = forumsManager;
		}

		public async Task Invoke(Post post)
		{
			// Re-get the post, as it may not be fully populated
			post = await this.ForumsManager.GetForumPost(post.Id);

			if (post.IsApproved)
			{
				await post.CreateSubscriptionEmail(this.ForumsManager);
			}
			else
			{
				await post.CreateModerationRequiredEmail(this.ForumsManager);
			}			
		}
	}
}

