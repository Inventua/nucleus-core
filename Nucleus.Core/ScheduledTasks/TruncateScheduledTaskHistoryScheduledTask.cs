using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.TaskScheduler;

namespace Nucleus.Core.ScheduledTasks
{
	[System.ComponentModel.DisplayName("Nucleus Core: Truncate Scheduled Task History")]
	public class TruncateScheduledTaskHistoryScheduledTask : IScheduledTask
	{
		private IScheduledTaskManager ScheduledTaskManager { get; }

		public TruncateScheduledTaskHistoryScheduledTask(IScheduledTaskManager scheduledTaskManager)
		{
			this.ScheduledTaskManager = scheduledTaskManager;
		}

		public Task InvokeAsync(RunningTask task, IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken)
		{
			//return Task.Run(() => TruncateScheduledTaskHistory(progress));
			return TruncateScheduledTaskHistory(progress);
		}

		private async Task TruncateScheduledTaskHistory(IProgress<ScheduledTaskProgress> progress)
		{
			foreach (ScheduledTask task in await this.ScheduledTaskManager.List())
			{
				await this.ScheduledTaskManager.TruncateHistory(task);
			}

			progress.Report(new ScheduledTaskProgress() { Status = ScheduledTaskProgress.State.Succeeded });
		}

	}
}
