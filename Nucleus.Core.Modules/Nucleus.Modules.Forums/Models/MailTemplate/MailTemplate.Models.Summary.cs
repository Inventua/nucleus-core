using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;

namespace Nucleus.Modules.Forums.Models.MailTemplate;

[MailTemplateDataModel()]
[System.ComponentModel.DisplayName("Forums - Summary Notification")]
public class Summary
	{
  public List<ForumSummary> Forums { get; } = new();
  public Site Site { get; set; }
  public Page Page { get; set; }
  public User User { get; set; }
  public string ForumNames { get; set; }
}
