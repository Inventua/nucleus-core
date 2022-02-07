using System;
using System.Threading;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core.ScheduledTasks
{
	public class CollectSessionsScheduledTask : IScheduledTask
	{
		private ISessionManager SessionManager { get; }

		public CollectSessionsScheduledTask(ISessionManager sessionManager)
		{
			this.SessionManager = sessionManager;
		}

		public Task InvokeAsync(RunningTask task, IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken)
		{
			return Task.Run(async () => await CollectExpiredSessions(progress));			
		}
		
		private async Task CollectExpiredSessions(IProgress<ScheduledTaskProgress> progress)
		{
			await this.SessionManager.DeleteExpiredSessions();
			progress.Report(new ScheduledTaskProgress() { Status = ScheduledTaskProgress.State.Succeeded });
		}
	}
}
