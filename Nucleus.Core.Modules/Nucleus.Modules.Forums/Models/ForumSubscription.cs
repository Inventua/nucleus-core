using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.Models
{
	public class ForumSubscription : ModelBase
	{
		public Guid UserId { get; set; }
		public Guid ForumId { get; set; }
	}
}
