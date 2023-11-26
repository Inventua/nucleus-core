using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nucleus.Modules.Forums.Models.MailQueue;

namespace Nucleus.Modules.Forums.Models
{
  public enum NotificationFrequency : int
  {
    Summary = 0,
    Single = 1
  }

  public class ForumSubscription : ModelBase
	{
    public Guid UserId { get; set; }
		public User User { get; set; }
		public Guid ForumId { get; set; }
    public NotificationFrequency? NotificationFrequency { get; set; }

    public ForumSubscription()
    {
      this.NotificationFrequency = Models.NotificationFrequency.Summary;
    }
  }
}
