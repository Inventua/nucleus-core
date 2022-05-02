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

namespace Nucleus.Modules.Forums
{
	public class ForumsEventHandler : 
		Nucleus.Abstractions.EventHandlers.ISystemEventHandler<Post, Create>,
		Nucleus.Abstractions.EventHandlers.ISystemEventHandler<Post, Approved>,
		Nucleus.Abstractions.EventHandlers.ISystemEventHandler<Reply, Create>,
		Nucleus.Abstractions.EventHandlers.ISystemEventHandler<Reply, Approved>
	{		
		private ForumsManager ForumsManager { get; }

		public ForumsEventHandler(ForumsManager forumsManager)
		{
			this.ForumsManager = forumsManager;
		}

		public async Task Invoke(Post post)
		{
			if (post.IsApproved)
			{
				Forum forum = await this.ForumsManager.Get(post.ForumId);
				if (forum == null) return;

				if (forum.EffectiveSettings().SubscriptionMailTemplateId.HasValue)
				{
					List<User> subscribers = await this.ForumsManager.ListForumSubscribers(post.ForumId);

					foreach (User subscriber in subscribers)
					{
						MailQueue item = new()
						{
							ModuleId = forum.Group.ModuleId,
							UserId = subscriber.Id,
							Post = post,
							Reply = null
						};

						await this.ForumsManager.SaveMailQueue(item);
					}
				}
			}		
		}

		public async Task Invoke(Reply reply)
		{
			if (reply.IsApproved)
			{
				{
					Forum forum = await this.ForumsManager.Get(reply.Post.ForumId);
					if (forum == null) return;

					if (forum.EffectiveSettings().SubscriptionMailTemplateId.HasValue)
					{
						List<User> subscribers = await this.ForumsManager.ListPostSubscribers(reply.Id);

						foreach (User subscriber in subscribers)
						{
							MailQueue item = new()
							{
								ModuleId = forum.Group.ModuleId,
								UserId = subscriber.Id,
								Post = reply.Post,
								Reply = reply
							};

							await this.ForumsManager.SaveMailQueue(item);
						}
					}
				}
			}
			
		}
	}
}

