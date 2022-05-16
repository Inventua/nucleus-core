using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Modules.Forums.Models;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;

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
				List<User> subscribers = await forumsManager.ListForumSubscribers(post.ForumId);

				foreach (User subscriber in subscribers)
				{
					// do not send a notification to the person who posted the message
					if (subscriber.Id != post.AddedBy)
					{
						// do not send notifications to un-approved or un-verified users
						if (subscriber.Approved && subscriber.Verified)
						{
							MailQueue item = new()
							{
								ModuleId = forum.Group.ModuleId,
								UserId = subscriber.Id,
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
				List<User> subscribers = await forumsManager.ListPostSubscribers(reply.Post.Id);

				foreach (User subscriber in subscribers)
				{
					// do not send a notification to the person who posted the reply
					if (subscriber.Id != reply.AddedBy)
					{
						// do not send notifications to un-approved or un-verified users
						if (subscriber.Approved && subscriber.Verified)
						{
							MailQueue item = new()
							{
								ModuleId = forum.Group.ModuleId,
								UserId = subscriber.Id,
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
