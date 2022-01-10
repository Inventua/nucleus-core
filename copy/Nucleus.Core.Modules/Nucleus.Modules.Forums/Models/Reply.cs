using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.Models
{
	public class Reply : Nucleus.Abstractions.Models.ModelBase
	{
		public Guid Id { get; set; }
		public Guid ForumPostId { get; set; }
		public Guid ReplyToId { get; set; }
		public string Body { get; set; }

		public Boolean IsApproved { get; set; }

		public string PostedBy { get; set; }
		public List<Attachment> Attachments { get; set; } = new();
	}
}
