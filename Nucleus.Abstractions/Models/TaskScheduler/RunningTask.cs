using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Nucleus.Abstractions.Models.TaskScheduler
{
	/// <summary>
	/// Represents a scheduled task that is running.
	/// </summary>
	public class RunningTask
	{
		/// <summary>
		/// Delegate for the OnProgress event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="progress"></param>
		public delegate void ProgressEvent(RunningTask sender, ScheduledTaskProgress progress);

		/// <summary>
		/// Event raised when progress is reported by the task.
		/// </summary>
		public event ProgressEvent OnProgress;

		/// <summary>
		/// The ScheduledTask which contains user(administrator) settings for the task.
		/// </summary>
		public ScheduledTask ScheduledTask { get; }

		/// <summary>
		/// History item for the current task instance
		/// </summary>
		public ScheduledTaskHistory History { get; }

		/// <summary>
		/// The System.Threading.Tasks.Task
		/// </summary>
		public Task Task { get; set; }

		/// <summary>
		/// The current progress of the task.
		/// </summary>
		public ScheduledTaskProgress Progress { get; private set; } = new();

		/// <summary>
		/// Progress update callback provider
		/// </summary>
		public Progress<ScheduledTaskProgress> ProgressCallback { get; private set; }

		/// <summary>
		/// Date/time that the task was started
		/// </summary>
		public DateTime StartDate { get; }

		/// <summary>
		/// Initializes a new instance with the specified ScheduledTask
		/// </summary>
		/// <param name="scheduledTask"></param>
		/// <param name="history"></param>
		public RunningTask(ScheduledTask scheduledTask, ScheduledTaskHistory history)
		{
			this.StartDate = DateTime.UtcNow;
			this.ScheduledTask = scheduledTask;
			this.History = history;
			this.ProgressCallback = new Progress<ScheduledTaskProgress>();
			this.ProgressCallback.ProgressChanged += this.HandleProgress;
		}

		/// <summary>
		/// Updates the current progress of the task.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="progress"></param>
		public void HandleProgress(object sender, ScheduledTaskProgress progress)
		{
			this.Progress = progress;
			this.History.Status = progress.Status;
			this.OnProgress?.Invoke(this, progress);
		}
	}
}
