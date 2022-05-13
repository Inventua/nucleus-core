using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.Models
{
	public class UserSubscriptions
	{
		public IList<Forum> Forums { get; set; } = new List<Forum>();
		public IList<Post> Posts { get; set; } = new List<Post>();
	}
}
