using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.Models
{
	public enum FlagStates
	{
		IsTrue,
		IsFalse,
		IsAny
	}

	public class Post : Nucleus.Abstractions.Models.ModelBase
	{
		public Guid Id { get; set; }
		public Guid ForumId { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }

		public Boolean IsLocked { get; set; }
		public Boolean IsPinned { get; set; }
		public Boolean IsApproved { get; set; }

		public ListItem Status { get; set; }

		public PostStatistics Statistics { get; set; }
		public List<Attachment> Attachments { get; set; } = new();
		public string PostedBy { get; set; }
	}
}
