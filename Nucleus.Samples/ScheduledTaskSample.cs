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
using Nucleus.Core.Logging;

namespace Nucleus.Samples
{
	public class SampleScheduledTask : IScheduledTask
	{
		private ILogger<SampleScheduledTask> Logger { get; }
		private RunningTask TaskInfo { get; set; }
		IProgress<ScheduledTaskProgress> Progress { get; set; }
		CancellationToken CancellationToken { get; set; }

		public SampleScheduledTask(ILogger<SampleScheduledTask> logger)
		{
			this.Logger = logger;			
		}

		public Task InvokeAsync(RunningTask taskInfo, IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken)
		{
			this.TaskInfo = taskInfo;
			this.Progress = progress;
			this.CancellationToken = cancellationToken;

			return Task.Run(DoWork);			
		}

		private async Task DoWork()
		{
			this.Logger.LogInformation(this.TaskInfo, "Starting Sample Scheduled Task work");
			this.Progress.Report(ScheduledTaskProgress.Running());

			await Task.Delay(60000, this.CancellationToken);
			this.Logger.LogInformation(this.TaskInfo, "Finished Sample Scheduled Task work");
			this.Progress.Report(ScheduledTaskProgress.Success());
		}

	}
}
