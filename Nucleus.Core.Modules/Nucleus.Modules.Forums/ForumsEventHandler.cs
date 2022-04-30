using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;
using Nucleus.Modules.Forums.Models;

namespace Nucleus.Modules.Forums
{
	public class ForumsEventHandler : 
		Nucleus.Abstractions.EventHandlers.ISystemEventHandler<Post, Create>,
		Nucleus.Abstractions.EventHandlers.ISystemEventHandler<Reply, Create>
	{		
		public Task Invoke(Post post)
		{
			return Task.CompletedTask;
		}

		public Task Invoke(Reply reply)
		{
			return Task.CompletedTask;
		}
	}
}

