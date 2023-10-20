using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Modules.Forums.Models;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Microsoft.Extensions.Hosting;

namespace Nucleus.Modules.Forums.EventHandlers
{
	internal static class EventHandlerExtensions
	{
		public static async Task CreateSubscriptionEmail(this Post post, ForumsManager forumsManager)
		{
			Forum forum = await forumsManager.Get(post.ForumId);
			if (forum == null) return;

			if (forum.EffectiveSettings().SubscriptionMailTemplateId.HasValue)
			{
				List<ForumSubscription> subscriptions = await forumsManager.ListForumSubscribers(post.ForumId);

				foreach (ForumSubscription subscription in subscriptions)
				{
					// do not send a notification to the person who posted the message
					if (subscription.User.Id != post.AddedBy)
					{
						// do not send notifications to un-approved or un-verified users, or users who were not created yet when the subscription was created.  This
            // extra check is to prevent emails for imported forum posts.
						if (subscription.User.Approved && subscription.User.Verified && post.DateAdded >= subscription.DateAdded)
						{
							MailQueue item = new()
							{
								ModuleId = forum.Group.ModuleId,
								UserId = subscription.User.Id,
								MailTemplateId = forum.EffectiveSettings().SubscriptionMailTemplateId.Value,
								Post = post,
								Reply = null
							};

							await forumsManager.SaveMailQueue(item);
						}
					}
				}
			}
		}

		public static async Task CreateSubscriptionEmail(this Reply reply, ForumsManager forumsManager)
		{
			Forum forum = await forumsManager.Get(reply.Post.ForumId);
			if (forum == null) return;

			if (forum.EffectiveSettings().SubscriptionMailTemplateId.HasValue)
			{
				List<PostSubscription> subscriptions = await forumsManager.ListPostSubscribers(reply.Post.Id);

				foreach (PostSubscription subscription in subscriptions)
				{
					// do not send a notification to the person who posted the reply
					if (subscription.User.Id != reply.AddedBy)
					{
            // do not send notifications to un-approved or un-verified users, or users who were not created yet when the subscription was created.  This
            // extra check is to prevent emails for imported forum replies.
            if (subscription.User.Approved && subscription.User.Verified && reply.DateAdded >= subscription.DateAdded)
            {
							MailQueue item = new()
							{
								ModuleId = forum.Group.ModuleId,
								UserId = subscription.User.Id,
								MailTemplateId = forum.EffectiveSettings().SubscriptionMailTemplateId.Value,
								Post = reply.Post,
								Reply = reply
							};

							await forumsManager.SaveMailQueue(item);
						}
					}
				}
			}
		}


		public static async Task CreateModerationRequiredEmail(this Post post, ForumsManager forumsManager)
		{
			Forum forum = await forumsManager.Get(post.ForumId);
			if (forum == null) return;

			if (forum.EffectiveSettings().ModerationRequiredMailTemplateId.HasValue)
			{
				IList<User> moderators = await forumsManager.ListForumModerators(forum);

				foreach (User moderator in moderators)
				{
					MailQueue item = new()
					{
						ModuleId = forum.Group.ModuleId,
						UserId = moderator.Id,
						MailTemplateId = forum.EffectiveSettings().ModerationRequiredMailTemplateId.Value,
						Post = post,
						Reply = null
					};

					await forumsManager.SaveMailQueue(item);
				}
			}
		}

		public static async Task CreateModerationRequiredEmail(this Reply reply, ForumsManager forumsManager)
		{
			Forum forum = await forumsManager.Get(reply.Post.ForumId);
			if (forum == null) return;

			if (forum.EffectiveSettings().IsModerated && forum.EffectiveSettings().ModerationRequiredMailTemplateId.HasValue)
			{
				IList<User> moderators = await forumsManager.ListForumModerators(forum);

				foreach (User moderator in moderators)
				{
					MailQueue item = new()
					{
						ModuleId = forum.Group.ModuleId,
						UserId = moderator.Id,
						MailTemplateId = forum.EffectiveSettings().ModerationRequiredMailTemplateId.Value,
						Post = reply.Post,
						Reply = reply
					};

					await forumsManager.SaveMailQueue(item);
				}
			}

		}


		public static async Task CreateModerationApprovedEmail(this Post post, ForumsManager forumsManager, IUserManager userManager)
		{
			if (!post.AddedBy.HasValue) return;

			Forum forum = await forumsManager.Get(post.ForumId);
			User user = await userManager.Get(post.AddedBy.Value);

			if (forum == null || user == null) return;

			if (forum.EffectiveSettings().IsModerated && forum.EffectiveSettings().ModerationApprovedMailTemplateId.HasValue)
			{
				MailQueue item = new()
				{
					ModuleId = forum.Group.ModuleId,
					UserId = user.Id,
					MailTemplateId = forum.EffectiveSettings().ModerationApprovedMailTemplateId.Value,
					Post = post,
					Reply = null
				};

				await forumsManager.SaveMailQueue(item);
			}
		}

		public static async Task CreateModerationApprovedEmail(this Reply reply, ForumsManager forumsManager, IUserManager userManager)
		{
			if (!reply.AddedBy.HasValue) return;

			Forum forum = await forumsManager.Get(reply.Post.ForumId);
			User user = await userManager.Get(reply.AddedBy.Value);

			if (forum == null || user == null) return;

			if (forum.EffectiveSettings().IsModerated && forum.EffectiveSettings().ModerationApprovedMailTemplateId.HasValue)
			{
				MailQueue item = new()
				{
					ModuleId = forum.Group.ModuleId,
					UserId = user.Id,
					MailTemplateId = forum.EffectiveSettings().ModerationApprovedMailTemplateId.Value,
					Post = reply.Post,
					Reply = reply
				};

				await forumsManager.SaveMailQueue(item);

			}
		}

		public static async Task CreateModerationRejectedEmail(this Post post, ForumsManager forumsManager, IUserManager userManager)
		{
			if (!post.AddedBy.HasValue) return;

			Forum forum = await forumsManager.Get(post.ForumId);
			User user = await userManager.Get(post.AddedBy.Value);

			if (forum == null || user == null) return;

			if (forum.EffectiveSettings().IsModerated && forum.EffectiveSettings().ModerationRejectedMailTemplateId.HasValue)
			{
				MailQueue item = new()
				{
					ModuleId = forum.Group.ModuleId,
					UserId = user.Id,
					MailTemplateId = forum.EffectiveSettings().ModerationRejectedMailTemplateId.Value,
					Post = post,
					Reply = null
				};

				await forumsManager.SaveMailQueue(item);

			}

		}

		public static async Task CreateModerationRejectedEmail(this Reply reply, ForumsManager forumsManager, IUserManager userManager)
		{
			if (!reply.AddedBy.HasValue) return;

			Forum forum = await forumsManager.Get(reply.Post.ForumId);
			User user = await userManager.Get(reply.AddedBy.Value);

			if (forum == null || user == null) return;

			if (forum.EffectiveSettings().IsModerated && forum.EffectiveSettings().ModerationRejectedMailTemplateId.HasValue)
			{
				MailQueue item = new()
				{
					ModuleId = forum.Group.ModuleId,
					UserId = user.Id,
					MailTemplateId = forum.EffectiveSettings().ModerationRejectedMailTemplateId.Value,
					Post = reply.Post,
					Reply = reply
				};

				await forumsManager.SaveMailQueue(item);
			}
		}
	}
}
