using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.Models
{
  public class UserSubscriptions
  {
    public IList<ForumGroupUserSubscription> GroupSubscriptions { get; set; } = new List<ForumGroupUserSubscription>();
    public IList<ForumUserSubscription> ForumSubscriptions { get; set; } = new List<ForumUserSubscription>();
    public IList<PostUserSubscription> PostSubscriptions { get; set; } = new List<PostUserSubscription>();

    
    public NotificationFrequency GetGroupSubscriptionNotificationFrequency(Guid groupId)
    {
      NotificationFrequency? value = this.GroupSubscriptions.Where(subscription => subscription.Group.Id == groupId).FirstOrDefault()?.Subscription.NotificationFrequency;
      if (!value.HasValue)
      {
        value = NotificationFrequency.Summary;
      }

      return value.Value;
    }

    public NotificationFrequency GetForumSubscriptionNotificationFrequency(Guid forumId)
    {
      NotificationFrequency? value = this.ForumSubscriptions.Where(subscription => subscription.Forum.Id == forumId).FirstOrDefault()?.Subscription.NotificationFrequency;
      if (!value.HasValue)
      {
        value = NotificationFrequency.Summary;
      }
      
      return value.Value;
    }

    public class ForumGroupUserSubscription
    {
      public Group Group { get; set; }  
      public ForumGroupSubscription Subscription { get; set; }
    }

    public class ForumUserSubscription
    {
      public Forum Forum { get; set; }
      public ForumSubscription Subscription { get; set; }
    }

    public class PostUserSubscription
    {
      public Post Post { get; set; }
      public PostSubscription Subscription { get; set; }
    }

  }
}
