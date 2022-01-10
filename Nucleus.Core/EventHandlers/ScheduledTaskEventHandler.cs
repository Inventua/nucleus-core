using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Abstractions.EventHandlers;
using Nucleus.Abstractions.EventHandlers.SystemEventTypes;

namespace Nucleus.Core.EventHandlers
{
	public class ScheduledTaskEventHandler : ISystemEventHandler<ScheduledTask, Update>
	{
		/// <summary>
		/// Perform operations after a page is created.
		/// </summary>
		/// <param name="scheduledTask"></param>
		/// <remarks>
		/// This class has no implementation and is currently used for testing.
		/// </remarks>
		public Task Invoke(ScheduledTask scheduledTask)
		{
			return Task.CompletedTask;
		}
	}
}
