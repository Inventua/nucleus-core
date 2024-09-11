using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Forums.Models;

namespace Nucleus.Modules.Forums.EventHandlers;

internal static class EventHandlerExtensions
{
  public static async Task CreateSubscriptionEmail(this Post post, ForumsManager forumsManager, ILogger logger)
  {
    Forum forum = await forumsManager.Get(post.ForumId);

    if (forum == null)
    {
      logger?.LogForumNotFound(post.ForumId);
      return;
    }

    // forum group subscriptions
    List<ForumGroupSubscription> groupSubscriptions = await forumsManager.ListForumGroupSubscribers(forum.Group.Id);
    logger?.LogFoundSubscriptions("group subscriptions", groupSubscriptions.Count);

    foreach (ForumGroupSubscription subscription in groupSubscriptions)
    {
      logger?.LogProcessingUser("group subscription", subscription.UserId);
      if (forumsManager.HasPermission(subscription.User, forum, ForumsManager.PermissionScopes.FORUM_VIEW))
      {
        // do not send a notification to the person who posted the message
        if (subscription.User.Id != post.AddedBy)
        {
          // do not send notifications to un-approved or un-verified users, or users who were not created yet when the subscription was created.  This
          // extra check is to prevent emails for imported forum posts.
          if (subscription.User.Approved)
          {
            if (subscription.User.Verified)
            {
              if (post.DateAdded >= subscription.DateAdded)
              {
                if (subscription.User.Profile.Any(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email))
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
                    logger?.LogNotificationCreated("subscription notification", item.Id, subscription.UserId, "post", post.Id);
                  }
                  else
                  {
                    logger?.LogNoTemplateSpecified(forum.Id, "post", post.Id, "Subscription");
                  }
                }
                else
                {
                  logger?.LogNoEmail(subscription.UserId);
                }
              }
              else
              {
                logger?.LogSubscriptionNotYetValid(subscription.UserId);
              }
            }
            else
            {
              logger?.LogUserNotVerified(subscription.UserId);
            }
          }
          else
          {
            logger?.LogUserNotApproved(subscription.UserId);
          }
        }
        else
        {
          logger?.LogUserOwnPostOrReply("post", subscription.UserId);
        }
      }
      else
      {
        logger?.LogUserNoPermission("VIEW_PERMISSION", subscription.UserId, forum.Id);
      }
    }

    // forum subscriptions
    List<ForumSubscription> forumSubscriptions = await forumsManager.ListForumSubscribers(post.ForumId);
    logger?.LogFoundSubscriptions("forum subscriptions", forumSubscriptions.Count);

    foreach (ForumSubscription subscription in forumSubscriptions)
    {
      logger?.LogProcessingUser("forum subscription", subscription.UserId);
      // do not send a notification to the person who posted the message
      if (subscription.User.Id != post.AddedBy)
      {
        // do not send notifications to un-approved or un-verified users, or users who were not created yet when the subscription was created.  This
        // extra check is to prevent emails for imported forum posts.
        if (subscription.User.Approved)
        {
          if (subscription.User.Verified)
          {
            if (post.DateAdded >= subscription.DateAdded)
            {
              if (subscription.User.Profile.Any(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email))
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
                  logger?.LogNotificationCreated("subscription notification", item.Id, subscription.UserId, "post", post.Id);
                }
                else
                {
                  logger?.LogNoTemplateSpecified(forum.Id, "post", post.Id, "Subscription");
                }
              }
              else
              {
                logger?.LogNoEmail(subscription.UserId);
              }
            }
            else
            {
              logger?.LogSubscriptionNotYetValid(subscription.UserId);
            }
          }
          else
          {
            logger?.LogUserNotVerified(subscription.UserId);
          }
        }
        else
        {
          logger?.LogUserNotApproved(subscription.UserId);
        }
      }
      else
      {
        logger?.LogUserOwnPostOrReply("post", subscription.UserId);
      }

      // new posts can't have an post subscribers
    }
  }

  public static async Task CreateSubscriptionEmail(this Reply reply, ForumsManager forumsManager, ILogger logger)
  {
    Forum forum = await forumsManager.Get(reply.Post.ForumId);
    if (forum == null)
    {
      logger?.LogForumNotFound(reply.Post.ForumId);
      return;
    }

    // forum group subscriptions
    List<ForumGroupSubscription> groupSubscriptions = await forumsManager.ListForumGroupSubscribers(forum.Group.Id);
    logger?.LogFoundSubscriptions("group subscriptions", groupSubscriptions.Count);

    foreach (ForumGroupSubscription subscription in groupSubscriptions)
    {
      logger?.LogProcessingUser("group subscription", subscription.UserId);

      // do not send a notification to the person who posted the reply
      if (subscription.User.Id != reply.AddedBy)
      {
        // do not send notifications to un-approved or un-verified users, or users who were not created yet when the subscription was created.  This
        // extra check is to prevent emails for imported forum replies.
        if (subscription.User.Approved)
        {
          if (subscription.User.Verified)
          {
            if (reply.DateAdded >= subscription.DateAdded)
            {
              if (subscription.User.Profile.Any(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email))
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
                  logger?.LogNotificationCreated("subscription notification", item.Id, subscription.UserId, "reply", reply.Id);
                }
                else
                {
                  logger?.LogNoTemplateSpecified(forum.Id, "reply", reply.Id, "Subscription");
                }
              }
              else
              {
                logger?.LogNoEmail(subscription.UserId);
              }
            }
            else
            {
              logger?.LogSubscriptionNotYetValid(subscription.UserId);
            }
          }
          else
          {
            logger?.LogUserNotVerified(subscription.UserId);
          }
        }
        else
        {
          logger?.LogUserNotApproved(subscription.UserId);
        }
      }
      else
      {
        logger?.LogUserOwnPostOrReply("reply", subscription.UserId);
      }
    }

    // forum subscriptions
    List<ForumSubscription> forumSubscriptions = await forumsManager.ListForumSubscribers(reply.Post.ForumId);
    logger?.LogFoundSubscriptions("forum subscriptions", forumSubscriptions.Count);

    foreach (ForumSubscription subscription in forumSubscriptions)
    {
      logger?.LogProcessingUser("forum subscription", subscription.UserId);

      // do not send a notification to the person who posted the reply
      if (subscription.User.Id != reply.AddedBy)
      {
        // do not send notifications to un-approved or un-verified users, or users who were not created yet when the subscription was created.  This
        // extra check is to prevent emails for imported forum replies.
        if (subscription.User.Approved)
        {
          if (subscription.User.Verified)
          {
            if (reply.DateAdded >= subscription.DateAdded)
            {
              if (subscription.User.Profile.Any(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email))
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
                  logger?.LogNotificationCreated("subscription notification", item.Id, subscription.UserId, "reply", reply.Id);
                }
                else
                {
                  logger?.LogNoTemplateSpecified(forum.Id, "reply", reply.Id, "Subscription");
                }
              }
              else
              {
                logger?.LogNoEmail(subscription.UserId);
              }
            }
            else
            {
              logger?.LogSubscriptionNotYetValid(subscription.UserId);
            }
          }
          else
          {
            logger?.LogUserNotVerified(subscription.UserId);
          }
        }
        else
        {
          logger?.LogUserNotApproved(subscription.UserId);
        }
      }
      else
      {
        logger?.LogUserOwnPostOrReply("reply", subscription.UserId);
      }
    }

    // post subscriptions
    List<PostSubscription> postSubscriptions = await forumsManager.ListPostSubscribers(reply.Post.Id);
    logger?.LogFoundSubscriptions("post subscriptions", postSubscriptions.Count);

    foreach (PostSubscription subscription in postSubscriptions)
    {
      logger?.LogProcessingUser("post subscription", subscription.UserId);

      // do not send a notification to the person who posted the reply
      if (subscription.User.Id != reply.AddedBy)
      {
        // do not send notifications to un-approved or un-verified users, or users who were not created yet when the subscription was created.  This
        // extra check is to prevent emails for imported forum replies.
        if (subscription.User.Approved)
        {
          if (subscription.User.Verified)
          {
            if (reply.DateAdded >= subscription.DateAdded)
            {
              if (subscription.User.Profile.Any(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email))
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
                else
                {
                  logger?.LogNoTemplateSpecified(forum.Id, "reply", reply.Id, "Subscription");
                }
              }
              else
              {
                logger?.LogNoEmail(subscription.UserId);
              }
            }
            else
            {
              logger?.LogSubscriptionNotYetValid(subscription.UserId);
            }
          }
          else
          {
            logger?.LogUserNotVerified(subscription.UserId);
          }
        }
        else
        {
          logger?.LogUserNotApproved(subscription.UserId);
        }
      }
      else
      {
        logger?.LogUserOwnPostOrReply("reply", subscription.UserId);
      }
    }
  }

  public static async Task CreateModerationRequiredEmail(this Post post, ForumsManager forumsManager, ILogger logger)
  {
    Forum forum = await forumsManager.Get(post.ForumId);

    if (forum == null)
    {
      logger?.LogForumNotFound(post.ForumId);
      return;
    }

    if (forum.EffectiveSettings().IsModerated)
    {
      if (forum.EffectiveSettings().ModerationRequiredMailTemplateId.HasValue)
      {
        IList<User> moderators = await forumsManager.ListForumModerators(forum);
        logger?.LogFoundSubscriptions("moderators" ,moderators.Count);

        foreach (User moderator in moderators)
        {
          logger?.LogProcessingUser("moderatoration required", moderator.Id);

          if (moderator.Profile.Any(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email))
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
            logger?.LogNotificationCreated("moderation required notification", item.Id, moderator.Id, "post", post.Id);
          }
          else
          {
            logger?.LogNoEmail(moderator.Id);
          }
        }
      }
      else
      {
        logger?.LogNoTemplateSpecified(forum.Id, "post", post.Id, "Moderation Required");
      }
    }
    else
    {
      logger?.LogNotModerated(forum.Id, "post", post.Id);
    }
  }

  public static async Task CreateModerationRequiredEmail(this Reply reply, ForumsManager forumsManager, ILogger logger)
  {
    Forum forum = await forumsManager.Get(reply.Post.ForumId);
    if (forum == null)
    {
      logger?.LogForumNotFound(reply.Post.ForumId);
      return;
    }

    if (forum.EffectiveSettings().IsModerated)
    {
      if (forum.EffectiveSettings().ModerationRequiredMailTemplateId.HasValue)
      {
        IList<User> moderators = await forumsManager.ListForumModerators(forum);
        logger?.LogFoundSubscriptions("moderators", moderators.Count);

        foreach (User moderator in moderators)
        {
          logger?.LogProcessingUser("moderatoration required", moderator.Id);

          if (moderator.Profile.Any(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email))
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
            logger?.LogNotificationCreated("moderation required notification", item.Id, moderator.Id, "reply", reply.Id);
          }
          else
          {
            logger?.LogNoEmail(moderator.Id);
          }
        }
      }
      else
      {
        logger?.LogNoTemplateSpecified(forum.Id, "reply", reply.Id, "Moderation Required");
      }
    }
    else
    {
      logger?.LogNotModerated(forum.Id, "reply", reply.Id);
    }
  }

  public static async Task CreateModerationApprovedEmail(this Post post, ForumsManager forumsManager, IUserManager userManager, ILogger logger)
  {
    if (!post.AddedBy.HasValue) return;

    Forum forum = await forumsManager.Get(post.ForumId);
    User user = await userManager.Get(post.AddedBy.Value);

    if (forum == null)
    {
      logger?.LogForumNotFound(post.ForumId);
      return;
    }

    if (user == null)
    {
      logger?.LogUserNotFound(post.AddedBy.Value);
      return;
    }

    // don't send "post approved" emails to users who are not moderated
    if (forum.EffectiveSettings().IsModerated)
    {
      if (forumsManager.HasPermission(user, forum, ForumsManager.PermissionScopes.FORUM_UNMODERATED))
      {
        // don't send anything to users with no email address
        if (user.Profile.Any(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email))
        {
          if (forum.EffectiveSettings().ModerationApprovedMailTemplateId.HasValue)
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
            logger?.LogNotificationCreated("post approved notification", item.Id, user.Id, "post", post.Id);
          }
          else
          {
            logger?.LogNoTemplateSpecified(forum.Id, "post", post.Id, "Approved");
          }
        }
        else
        {
          logger?.LogNoEmail(user.Id);
        }
      }
    }
    else
    {
      logger?.LogNotModerated(forum.Id, "post", post.Id);
    }
  }

  public static async Task CreateModerationApprovedEmail(this Reply reply, ForumsManager forumsManager, IUserManager userManager, ILogger logger)
  {
    if (!reply.AddedBy.HasValue) return;

    Forum forum = await forumsManager.Get(reply.Post.ForumId);
    User user = await userManager.Get(reply.AddedBy.Value);

    if (forum == null)
    {
      logger?.LogForumNotFound(reply.Post.ForumId);
      return;
    }

    if (user == null)
    {
      logger?.LogUserNotFound(reply.AddedBy.Value);
      return;
    }

    // don't send "reply approved" emails to users who are not moderated
    if (forumsManager.HasPermission(user, forum, ForumsManager.PermissionScopes.FORUM_UNMODERATED))
    {
      logger?.LogUserNoPermission("FORUM_UNMODERATED", user.Id, reply.Id);
      return;
    }

    // don't send anything to users with no email address
    if (user.Profile.Any(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email))
    {
      if (forum.EffectiveSettings().IsModerated)
      {
        if (forum.EffectiveSettings().ModerationApprovedMailTemplateId.HasValue)
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
          logger?.LogNotificationCreated("reply approved notification", item.Id, user.Id, "reply", reply.Id);
        }
        else
        {
          logger?.LogNoTemplateSpecified(forum.Id, "reply", reply.Id, "Approved");
        }
      }
      else
      {
        logger?.LogNotModerated(forum.Id, "reply", reply.Id);
      }
    }
    else
    {
      logger?.LogNoEmail(user.Id);
    }
  }

  public static async Task CreateModerationRejectedEmail(this Post post, ForumsManager forumsManager, IUserManager userManager, ILogger logger)
  {
    if (!post.AddedBy.HasValue) return;

    Forum forum = await forumsManager.Get(post.ForumId);
    User user = await userManager.Get(post.AddedBy.Value);

    if (forum == null)
    {
      logger?.LogForumNotFound(post.ForumId);
      return;
    }

    if (user == null)
    {
      logger?.LogUserNotFound(post.AddedBy.Value);
      return;
    }

    // don't send anything to users with no email address
    if (user.Profile.Any(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email))
    {
      if (forum.EffectiveSettings().IsModerated)
      {
        if (forum.EffectiveSettings().ModerationRejectedMailTemplateId.HasValue)
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
          logger?.LogNotificationCreated("post rejected notification", item.Id, user.Id, "post", post.Id);
        }
        else
        {
          logger?.LogNoTemplateSpecified(forum.Id, "post", post.Id, "Rejected");
        }
      }
      else
      {
        logger?.LogNotModerated(forum.Id, "post", post.Id);
      }
    }
    else
    {
      logger?.LogNoEmail(user.Id);
    }
  }

  public static async Task CreateModerationRejectedEmail(this Reply reply, ForumsManager forumsManager, IUserManager userManager, ILogger logger)
  {
    if (!reply.AddedBy.HasValue) return;

    Forum forum = await forumsManager.Get(reply.Post.ForumId);
    User user = await userManager.Get(reply.AddedBy.Value);

    if (forum == null)
    {
      logger?.LogForumNotFound(reply.Post.ForumId);
      return;
    }

    if (user == null)
    {
      logger?.LogUserNotFound(reply.AddedBy.Value);
      return;
    }

    // don't send anything to users with no email address
    if (user.Profile.Any(item => item.UserProfileProperty.TypeUri == System.Security.Claims.ClaimTypes.Email))
    {
      if (forum.EffectiveSettings().IsModerated)
      {
        if (forum.EffectiveSettings().ModerationRejectedMailTemplateId.HasValue)
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
          logger?.LogNotificationCreated("reply rejected notification", item.Id, user.Id, "reply", reply.Id);
        }
        else
        {
          logger?.LogNoTemplateSpecified(forum.Id, "reply", reply.Id, "Rejected");
        }
      }
      else
      {
        logger?.LogNotModerated(forum.Id, "reply", reply.Id);
      }
    }
    else
    {
      logger?.LogNoEmail(user.Id);
    }
  }
}
