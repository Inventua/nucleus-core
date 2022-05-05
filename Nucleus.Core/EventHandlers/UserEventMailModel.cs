using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Core.EventHandlers
{
	public class UserEventMailModel : Nucleus.Abstractions.Mail.MailTemplateModelBase<UserEventMailModel>
	{
		public Site Site { get; set; }
		public User User { get; set; }
	}
}
