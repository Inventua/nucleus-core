using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Modules.Forums.Models;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Modules.Forums.EventHandlers
{
	internal static class EventHandlerExtensions
	{
		public static async Task CreateSubscriptionEmail(this Post post, ForumsManager forumsManager)
		{
			Forum forum = await forumsManager.Get(post.ForumId);
			if (forum == null) return;

			// forum group subscriptions
			List<ForumGroupSubscription> groupSubscriptions = await forumsManager.ListForumGroupSubscribers(forum.Group.Id);

			foreach (ForumGroupSubscription subscription in groupSubscriptions)
			{
				if (forumsManager.HasPermission(subscription.User, forum, ForumsManager.PermissionScopes.FORUM_VIEW))
				{
					// do not send a notification to the person who posted the message
					if (subscription.User.Id != post.AddedBy)
					{
						// do not send notifications to un-approved or un-verified users, or users who were not created yet when the subscription was created.  This
						// extra check is to prevent emails for imported forum posts.
						if (subscription.User.Approved && subscription.User.Verified && post.DateAdded >= subscription.DateAdded && subscription.User.Profile.Where(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email).FirstOrDefault() != null)
						{
							Guid? templateId = subscription.NotificationFrequency == NotificationFrequency.Single ? forum.EffectiveSettings().SubscriptionMailTemplateId : forum.EffectiveSettings().SubscriptionSummaryMailTemplateId;
							if (templateId.HasValue)
							{
								MailQueue item = new()
								{
									ModuleId = forum.Group.ModuleId,
									UserId = subscription.User.Id,
									MailTemplateId = templateId.Value,
									Post = post,
									NotificationFrequency = subscription.NotificationFrequency ?? NotificationFrequency.Summary,
									Reply = null
								};

								await forumsManager.SaveMailQueue(item);
							}
						}
					}
				}
			}

			// forum subscriptions
			List<ForumSubscription> forumSubscriptions = await forumsManager.ListForumSubscribers(post.ForumId);

			foreach (ForumSubscription subscription in forumSubscriptions)
			{
				// do not send a notification to the person who posted the message
				if (subscription.User.Id != post.AddedBy)
				{
					// do not send notifications to un-approved or un-verified users, or users who were not created yet when the subscription was created.  This
					// extra check is to prevent emails for imported forum posts.
					if (subscription.User.Approved && subscription.User.Verified && post.DateAdded >= subscription.DateAdded && subscription.User.Profile.Where(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email).FirstOrDefault() != null)
					{
						Guid? templateId = subscription.NotificationFrequency == NotificationFrequency.Single ? forum.EffectiveSettings().SubscriptionMailTemplateId : forum.EffectiveSettings().SubscriptionSummaryMailTemplateId;
						if (templateId.HasValue)
						{
							MailQueue item = new()
							{
								ModuleId = forum.Group.ModuleId,
								UserId = subscription.User.Id,
								MailTemplateId = templateId.Value,
								NotificationFrequency = subscription.NotificationFrequency ?? NotificationFrequency.Summary,
								Post = post,
								Reply = null
							};

							await forumsManager.SaveMailQueue(item);
						}
					}
				}

				// new posts can't have an post subscribers
			}
		}

		public static async Task CreateSubscriptionEmail(this Reply reply, ForumsManager forumsManager)
		{
			Forum forum = await forumsManager.Get(reply.Post.ForumId);
			if (forum == null) return;

			// forum group subscriptions
			List<ForumGroupSubscription> groupSubscriptions = await forumsManager.ListForumGroupSubscribers(forum.Group.Id);

			foreach (ForumGroupSubscription subscription in groupSubscriptions)
			{
				// do not send a notification to the person who posted the reply
				if (subscription.User.Id != reply.AddedBy)
				{
					// do not send notifications to un-approved or un-verified users, or users who were not created yet when the subscription was created.  This
					// extra check is to prevent emails for imported forum replies.
					if (subscription.User.Approved && subscription.User.Verified && reply.DateAdded >= subscription.DateAdded && subscription.User.Profile.Where(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email).FirstOrDefault() != null)
					{
						Guid? templateId = subscription.NotificationFrequency == NotificationFrequency.Single ? forum.EffectiveSettings().SubscriptionMailTemplateId : forum.EffectiveSettings().SubscriptionSummaryMailTemplateId;
						if (templateId.HasValue)
						{
							MailQueue item = new()
							{
								ModuleId = forum.Group.ModuleId,
								UserId = subscription.User.Id,
								MailTemplateId = templateId.Value,
								NotificationFrequency = subscription.NotificationFrequency ?? NotificationFrequency.Summary,
								Post = reply.Post,
								Reply = reply
							};

							await forumsManager.SaveMailQueue(item);
						}
					}
				}
			}

			// forum subscriptions
			List<ForumSubscription> forumSubscriptions = await forumsManager.ListForumSubscribers(reply.Post.ForumId);

			foreach (ForumSubscription subscription in forumSubscriptions)
			{
				// do not send a notification to the person who posted the reply
				if (subscription.User.Id != reply.AddedBy)
				{
					// do not send notifications to un-approved or un-verified users, or users who were not created yet when the subscription was created.  This
					// extra check is to prevent emails for imported forum replies.
					if (subscription.User.Approved && subscription.User.Verified && reply.DateAdded >= subscription.DateAdded && subscription.User.Profile.Where(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email).FirstOrDefault() != null)
					{
						Guid? templateId = subscription.NotificationFrequency == NotificationFrequency.Single ? forum.EffectiveSettings().SubscriptionMailTemplateId : forum.EffectiveSettings().SubscriptionSummaryMailTemplateId;
						if (templateId.HasValue)
						{
							MailQueue item = new()
							{
								ModuleId = forum.Group.ModuleId,
								UserId = subscription.User.Id,
								MailTemplateId = templateId.Value,
								NotificationFrequency = subscription.NotificationFrequency ?? NotificationFrequency.Summary,
								Post = reply.Post,
								Reply = reply
							};

							await forumsManager.SaveMailQueue(item);
						}
					}
				}
			}

			// post subscriptions
			List<PostSubscription> postSubscriptions = await forumsManager.ListPostSubscribers(reply.Post.Id);

			foreach (PostSubscription subscription in postSubscriptions)
			{
				// do not send a notification to the person who posted the reply
				if (subscription.User.Id != reply.AddedBy)
				{
					// do not send notifications to un-approved or un-verified users, or users who were not created yet when the subscription was created.  This
					// extra check is to prevent emails for imported forum replies.
					if (subscription.User.Approved && subscription.User.Verified && reply.DateAdded >= subscription.DateAdded && subscription.User.Profile.Where(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email).FirstOrDefault() != null)
					{
						Guid? templateId = forum.EffectiveSettings().SubscriptionMailTemplateId;
						if (templateId.HasValue)
						{
							MailQueue item = new()
							{
								ModuleId = forum.Group.ModuleId,
								UserId = subscription.User.Id,
								MailTemplateId = templateId.Value,
								NotificationFrequency = NotificationFrequency.Single,
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

			if (forum.EffectiveSettings().IsModerated && forum.EffectiveSettings().ModerationRequiredMailTemplateId.HasValue)
			{
				IList<User> moderators = await forumsManager.ListForumModerators(forum);

				foreach (User moderator in moderators.Where(user => user.Profile.Where(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email).FirstOrDefault() != null))
				{

					MailQueue item = new()
					{
						ModuleId = forum.Group.ModuleId,
						UserId = moderator.Id,
						MailTemplateId = forum.EffectiveSettings().ModerationRequiredMailTemplateId.Value,
						NotificationFrequency = NotificationFrequency.Single,
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

				foreach (User moderator in moderators.Where(user => user.Profile.Where(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email).FirstOrDefault() != null))
				{
					MailQueue item = new()
					{
						ModuleId = forum.Group.ModuleId,
						UserId = moderator.Id,
						MailTemplateId = forum.EffectiveSettings().ModerationRequiredMailTemplateId.Value,
						NotificationFrequency = NotificationFrequency.Single,
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

			// don't send "post approved" emails to users who are not moderated
			if (forum.EffectiveSettings().IsModerated && forumsManager.HasPermission(user, forum, ForumsManager.PermissionScopes.FORUM_UNMODERATED)) return;

			// don't send anything to users with no email address
			if (user.Profile.Where(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email).FirstOrDefault() == null) return;

			if (forum.EffectiveSettings().IsModerated && forum.EffectiveSettings().ModerationApprovedMailTemplateId.HasValue)
			{
				MailQueue item = new()
				{
					ModuleId = forum.Group.ModuleId,
					UserId = user.Id,
					MailTemplateId = forum.EffectiveSettings().ModerationApprovedMailTemplateId.Value,
					NotificationFrequency = NotificationFrequency.Single,
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

			// don't send "reply approved" emails to users who are not moderated
			if (forumsManager.HasPermission(user, forum, ForumsManager.PermissionScopes.FORUM_UNMODERATED)) return;

			// don't send anything to users with no email address
			if (user.Profile.Where(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email).FirstOrDefault() == null) return;

			if (forum.EffectiveSettings().IsModerated && forum.EffectiveSettings().ModerationApprovedMailTemplateId.HasValue)
			{
				MailQueue item = new()
				{
					ModuleId = forum.Group.ModuleId,
					UserId = user.Id,
					MailTemplateId = forum.EffectiveSettings().ModerationApprovedMailTemplateId.Value,
					NotificationFrequency = NotificationFrequency.Single,
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

			// don't send anything to users with no email address
			if (user.Profile.Where(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email).FirstOrDefault() == null) return;

			if (forum.EffectiveSettings().IsModerated && forum.EffectiveSettings().ModerationRejectedMailTemplateId.HasValue)
			{
				MailQueue item = new()
				{
					ModuleId = forum.Group.ModuleId,
					UserId = user.Id,
					MailTemplateId = forum.EffectiveSettings().ModerationRejectedMailTemplateId.Value,
					NotificationFrequency = NotificationFrequency.Single,
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

			// don't send anything to users with no email address
			if (user.Profile.Where(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email).FirstOrDefault() == null) return;

			if (forum.EffectiveSettings().IsModerated && forum.EffectiveSettings().ModerationRejectedMailTemplateId.HasValue)
			{
				MailQueue item = new()
				{
					ModuleId = forum.Group.ModuleId,
					UserId = user.Id,
					MailTemplateId = forum.EffectiveSettings().ModerationRejectedMailTemplateId.Value,
					NotificationFrequency = NotificationFrequency.Single,
					Post = reply.Post,
					Reply = reply
				};

				await forumsManager.SaveMailQueue(item);
			}
		}
	}
}
