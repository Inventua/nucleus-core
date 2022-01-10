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
			return Task.Run(() => CollectExpiredSessions(progress));			
		}
		
		private void CollectExpiredSessions(IProgress<ScheduledTaskProgress> progress)
		{
			this.SessionManager.DeleteExpiredSessions();
			progress.Report(new ScheduledTaskProgress() { Status = ScheduledTaskProgress.State.Succeeded });
		}

	}
}
