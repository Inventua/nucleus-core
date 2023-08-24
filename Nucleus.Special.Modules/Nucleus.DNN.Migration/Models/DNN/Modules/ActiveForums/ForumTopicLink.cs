using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN.Modules.ActiveForums;

public class ForumTopicLink
{
  public int ForumId { get; set; }
  public int TopicId { get; set; }


  public ForumTopic Topic { get; set; }

  public Forum Forum { get; set; }

}
