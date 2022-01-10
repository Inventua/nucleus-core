using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.TaskScheduler;

namespace Nucleus.Core.ScheduledTasks
{
	public class TruncateScheduledTaskHistoryScheduledTask : IScheduledTask
	{
		private ScheduledTaskManager ScheduledTaskManager { get; }

		public TruncateScheduledTaskHistoryScheduledTask(ScheduledTaskManager scheduledTaskManager)
		{
			this.ScheduledTaskManager = scheduledTaskManager;
		}

		public Task InvokeAsync(RunningTask task, IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken)
		{
			return Task.Run(TruncateScheduledTaskHistory);
		}
		
		private void TruncateScheduledTaskHistory()
		{
			foreach (ScheduledTask task in this.ScheduledTaskManager.List())
			{
				this.ScheduledTaskManager.TruncateHistory(task);

			}
		}

	}
}
