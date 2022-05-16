using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Modules.Forums.Models
{
	
	public enum FlagStates
	{
		IsTrue,
		IsFalse,
		IsAny
	}

	public class Post : ModelBase
	{
		public const string URN = "urn:nucleus:entities:forum-post";

		public Guid Id { get; set; }
		public Guid ForumId { get; set; }

		[Required(ErrorMessage = "Please enter a subject.")]
		public string Subject { get; set; }

		[Required(ErrorMessage = "Please enter a body.")] 
		public string Body { get; set; }

		public Boolean IsLocked { get; set; }
		public Boolean IsPinned { get; set; }
		public Boolean IsApproved { get; set; }
		public Boolean? IsRejected { get; set; }

		public List<Reply> Replies { get; set; }

		public ListItem Status { get; set; }

		public PostStatistics Statistics { get; set; }
		public List<Attachment> Attachments { get; set; } = new();
		public PostTracking Tracking { get; set; }

		public User PostedBy { get; private set; }
	}
}
