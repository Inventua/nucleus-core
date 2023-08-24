using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN.Modules.ActiveForums;

public class ForumTopic
{
  public int TopicId { get; set; }

  public List<ForumTopicLink> TopicLinks { get; set; }

  public ForumContent Content { get; set; }

  public Boolean IsLocked { get; set; }
  public Boolean IsPinned { get; set; }
  public Boolean IsApproved { get; set; }
  public Boolean IsRejected{ get; set; }
  public Boolean IsDeleted { get; set; }
  public Boolean IsAnnounce { get; set; }
  public Boolean IsArchived { get; set; }

  //public List<ForumReply> Replies { get; set; }

}
