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
	public class Reply : ModelBase
	{
		public Guid Id { get; set; }

		public Post Post { get; private set; }

		public Reply ReplyTo { get; private set; }
		
		[Required]
		public string Body { get; set; }

		public Boolean IsApproved { get; set; }
		public Boolean? IsRejected { get; set; }


		public User PostedBy { get; private set; }

		public List<Attachment> Attachments { get; set; } = new();
	}
}
