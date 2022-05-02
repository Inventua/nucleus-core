using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;

namespace Nucleus.Modules.Forums.Models
{
	public class MailQueue
	{
		public enum MailQueueStatus
		{
			Queued=0,
			Sent=1
		}

		public Guid Id { get; set; }	
		public Guid ModuleId { get; set; }	
		public Guid UserId { get; set; }
		public Post Post { get; set; }
		public Reply Reply { get; set; }
		public MailQueueStatus Status { get; set; } = MailQueueStatus.Queued;
	}
}
