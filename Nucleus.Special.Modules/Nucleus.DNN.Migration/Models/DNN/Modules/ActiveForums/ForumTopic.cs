using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN.Modules.ActiveForums;

public class ForumTopic
{
  [Column("PostID")]
  public int PostId { get; set; }

  [Column("ForumID")]
  public int ForumId { get; set; }


  [Column("ParentPostID")]
  public int ParentPostId { get; set; }

  public ForumContent Content { get; set; }


  public List<ForumReply> Replies { get; set; }

}
