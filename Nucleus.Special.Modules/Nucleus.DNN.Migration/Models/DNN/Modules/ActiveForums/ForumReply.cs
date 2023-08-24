using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN.Modules.ActiveForums;

public class ForumReply
{
  [Column("ReplyID")]
  public int ReplyId { get; set; }

  public ForumTopic Topic { get; set; }

  public Boolean IsApproved { get; set; }
  public Boolean IsRejected { get; set; }
  public Boolean IsDeleted { get; set; }

  public ForumContent Content { get; set; }

}
