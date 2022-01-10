using Nucleus.Abstractions.Models.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.Models
{
	public class Settings
	{
		public Guid Id { get; set; }

		public Boolean Enabled { get; set; }
		public Boolean Visible { get; set; }

		public Boolean IsModerated { get; set; }
		public Boolean AllowAttachments { get; set; }
		public Boolean AllowSearchIndexing { get; set; }

		public Guid SubscriptionMailTemplateId { get; set; }
		public Guid ModerationRequiredMailTemplateId { get; set; }
		public Guid ModerationApprovedMailTemplateId { get; set; }
		public Guid ModerationRejectedMailTemplateId { get; set; }
		public Nucleus.Abstractions.Models.List StatusList { get; set; }

	}
}
