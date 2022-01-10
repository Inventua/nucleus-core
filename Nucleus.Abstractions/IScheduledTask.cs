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
