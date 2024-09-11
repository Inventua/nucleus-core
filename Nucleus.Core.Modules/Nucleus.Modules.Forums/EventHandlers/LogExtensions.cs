using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Nucleus.Modules.Forums.Models;

namespace Nucleus.Modules.Forums.EventHandlers;

internal static class LogExtensions
{
  
  internal static void LogForumNotFound(this ILogger logger, Guid forumId)
  {
    logger?.LogDebug("Forum id '{forumid}' not found.", forumId);    
  }

  internal static void LogUserNotFound(this ILogger logger, Guid userId)
  {
    logger?.LogDebug("User id '{userid}' not found.", userId);
  }

  internal static void LogFoundSubscriptions(this ILogger logger, string dataType, int count)
  {
    logger?.LogDebug("Found {count} {type}.", count, dataType);   
  }

  internal static void LogProcessingUser(this ILogger logger, string dataType, Guid userId)
  {
    logger?.LogDebug("Processing {dataType} for user id '{userid}'.", dataType, userId);   
  }

  internal static void LogNotificationCreated(this ILogger logger, string dataType, Guid id, Guid userId, string postOrReply, Guid postOrReplyId)
  {
    logger?.LogDebug("Created {dataType} '{id}' for user id '{userid}', {postOrReply} id '{postid}'.", dataType, id, userId, postOrReply, postOrReplyId);
  }

  internal static void LogNoTemplateSpecified(this ILogger logger, Guid forumId, string postOrReply, Guid postOrReplyId, string templateName)
  {
    logger?.LogDebug("Skipping notification for forum id '{forumId}', {postOrReply} '{postOrReplyId}' because the forum does not have a valid {templateName} template selected.", forumId, postOrReply, postOrReplyId, templateName);    
  }

  internal static void LogNoEmail(this ILogger logger, Guid userId)
  {
    logger?.LogDebug("Skipping notification for user id '{userid}' because the user does not have an email address.", userId);
  }

  internal static void LogSubscriptionNotYetValid(this ILogger logger, Guid userId)
  {
    logger?.LogDebug("Skipping notification for user id '{userid}' because their subscription date was after the post date.", userId);
  }

  internal static void LogUserNotVerified(this ILogger logger, Guid userId)
  {
    logger?.LogDebug("Skipping notification for user id '{userid}' because the user is not verified.", userId);
  }

  internal static void LogUserNotApproved(this ILogger logger, Guid userId)
  {
    logger?.LogDebug("Skipping notification for user id '{userid}' because the user is not approved.", userId);
  }

  internal static void LogUserOwnPostOrReply(this ILogger logger, string postOrReply, Guid userId)
  {
    logger?.LogDebug("Skipping notification for user id '{userid}' because the user created the {postOrReply}.", userId, postOrReply);
  }

  internal static void LogUserNoPermission(this ILogger logger, string permissionName, Guid userId, Guid forumId)
  {
    logger?.LogDebug("User id '{userid}' does not have {permissionName} permission for forum id '{forumid}'.", userId, permissionName, forumId);
  }

  internal static void LogNotModerated(this ILogger logger, Guid forumId, string postOrReply, Guid postOrReplyId)
  {
    logger?.LogDebug("Skipping notification for forum id '{forum}', {postOrReply} '{replyid}' because the forum is not moderated.", forumId, postOrReply, postOrReplyId);
  }

  
}
