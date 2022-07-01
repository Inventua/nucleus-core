using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Nucleus.Abstractions.Models.TaskScheduler;

namespace Nucleus.Abstractions
{
	/// <summary>
	/// Scheduled task class interface.
	/// </summary>
	/// <remarks>
	/// Scheduled Tasks implement the InvokeAsync method to perform their work. The implementation of InvokeAsync should execute asynchronously (either 
	/// return a Task object, or if your implementation doesn't use async methods, use Task.Run to call a function then return immediately) so that 
	/// scheduled tasks can run in parallel. The progress object should be used to report success or failure by calling progress.Report.
	/// </remarks>
	/// <seealso href="https://www.nucleus-cms.com/develop-extensions/scheduled-tasks/">Scheduled Tasks Documentation</seealso>
	public interface IScheduledTask
	{
		/// <summary>
		/// Execute the scheduled task logic.
		/// </summary>
		/// <param name="task"></param>
		/// <param name="progress"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public Task InvokeAsync(RunningTask task, IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken);
	}
}
