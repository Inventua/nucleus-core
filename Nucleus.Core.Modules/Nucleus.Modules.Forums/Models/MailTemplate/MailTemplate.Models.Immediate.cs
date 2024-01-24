using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;

namespace Nucleus.Modules.Forums.Models.MailTemplate.Models
{
  [MailTemplateDataModel()]
  [System.ComponentModel.DisplayName("Forums - Immediate Subscription, Moderation, Approval and Rejection Notifications")]
  public class Immediate
	{
		public Forum Forum { get; set; }
		public Site Site { get; set; }
		public Page Page { get; set; }
		public User User { get; set; }
    public string Summary { get; set; }
    public Post Post { get; set; }
    public Reply Reply { get; set; }
  }

}
