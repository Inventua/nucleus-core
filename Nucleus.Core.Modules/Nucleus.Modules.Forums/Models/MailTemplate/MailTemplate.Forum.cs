using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.Models.MailTemplate
{
	public class Forum
	{
		public string Name { get; private set; }
		public string Description { get; private set; }

		public List<Post> Posts { get; private set; } = new();
		public List<Reply> Replies { get; private set; } = new();

		public Forum(Nucleus.Modules.Forums.Models.Forum forum, List<Post> posts, List<Reply> replies)
		{
			this.Name = forum.Name;
			this.Description = forum.Description;
			this.Posts = posts;
			this.Replies = replies;
		}
	}
}
