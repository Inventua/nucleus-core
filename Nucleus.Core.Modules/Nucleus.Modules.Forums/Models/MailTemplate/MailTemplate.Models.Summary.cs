using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.Forums.Models.MailTemplate.Models
{
	public class Summary
	{
    public List<ForumSummary> Forums { get; } = new();
    public Site Site { get; set; }
    public Page Page { get; set; }
    public User User { get; set; }
    public string ForumNames { get; set; }
  }
}
