using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.Models
{
  public class ForumStatistics
  {
    public long PostCount { get; set; }
    public long ReplyCount { get; set; }
    public Post LastPost { get; set; }
    public Reply LastReply { get; set; }
		
	}
}
