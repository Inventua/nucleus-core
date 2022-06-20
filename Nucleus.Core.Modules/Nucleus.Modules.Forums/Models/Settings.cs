using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Modules.Forums.Models
{
	public class Settings : ModelBase
	{
		public Boolean Enabled { get; set; } = true;
		public Boolean Visible { get; set; } = true;

		public Boolean IsModerated { get; set; }
		public Boolean AllowAttachments { get; set; }
		public Boolean AllowSearchIndexing { get; set; }

		public Guid? SubscriptionMailTemplateId { get; set; }
		public Guid? ModerationRequiredMailTemplateId { get; set; }
		public Guid? ModerationApprovedMailTemplateId { get; set; }
		public Guid? ModerationRejectedMailTemplateId { get; set; }
		public Nucleus.Abstractions.Models.List StatusList { get; set; }
		public Folder AttachmentsFolder { get; set; }
	}
}
