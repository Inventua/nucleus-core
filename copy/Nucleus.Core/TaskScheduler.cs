using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.TaskScheduler;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions;

namespace Nucleus.Core
{
	/// <summary>
	/// Manages scheduling and execution of scheduled tasks.
	/// </summary>
	public class TaskScheduler : IHostedService, IDisposable	
	{
		// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-5.0&tabs=visual-studio

		private Timer Timer { get; set; }
		private ILogger<TaskScheduler> Logger { get; }
		private ScheduledTaskManager ScheduledTaskManager { get; }
		private IServiceProvider Services { get; }
		public RunningTaskQueue Queue { get; }
		public CancellationTokenSource CancellationTokenSource { get; } = new();

		public TaskScheduler(IServiceProvider services, ILogger<TaskScheduler> logger, ScheduledTaskManager scheduledTaskManager, RunningTaskQueue queue)
		{
			this.Services = services;
			this.Logger = logger;
			this.ScheduledTaskManager = scheduledTaskManager;
			this.Queue = queue;
		}

		/// <summary>
		/// Initialize the timer to start executing scheduled tasks.
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public Task StartAsync(CancellationToken cancellationToken)
		{
			this.Logger.LogInformation("The task scheduler is starting.");
			this.Timer = new(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
			return Task.CompletedTask;
		}

		/// <summary>
		/// Shut down
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public Task StopAsync(CancellationToken cancellationToken)
		{
			this.Logger.LogInformation("The task scheduler is stopping.");
			this.Timer?.Change(Timeout.Infinite, 0);
			this.CancellationTokenSource.Cancel();

			return Task.CompletedTask;
		}

		public void Dispose()
		{
			this.Timer?.Dispose();
			this.Timer = null;
		}

		/// <summary>
		/// Check for scheduled tasks that are ready to be invoked and start them.
		/// </summary>
		/// <param name="state"></param>
		void DoWork(object state)
		{
			Collect();

			foreach (ScheduledTask task in this.ScheduledTaskManager.List())
			{
				if (task.Enabled && task.NextScheduledRun <= DateTime.UtcNow)
				{
					if (!this.Queue.Contains(task))
					{
						StartScheduledTask(task);
					}
				}
			}
		}

		/// <summary>
		/// Check for completed tasks in the queue: reschedule then and remove them from the queue.
		/// </summary>
		private void Collect()
		{
			foreach (RunningTask queueItem in this.Queue.ToList())
			{
				// queueItem.Task can be null if the implementation of .InvokeAsync doesn't return anything
				if (queueItem.Task.IsCompleted)
				{
					// re-schedule the task
					RescheduleTask(queueItem.ScheduledTask, queueItem.ScheduledTask.NextScheduledRun);

					queueItem.History.FinishDate = DateTime.UtcNow;
					queueItem.History.NextScheduledRun = queueItem.ScheduledTask.NextScheduledRun;
					TaskSucceeded(queueItem.History);

					this.Queue.Remove(queueItem.ScheduledTask);
				}
			}
		}

		/// <summary>
		/// Update the <see cref="ScheduledTask.NextScheduledRun"/> based on task settings.
		/// </summary>
		/// <param name="thisTask"></param>
		/// <param name="thisRunDateTime"></param>
		void RescheduleTask(ScheduledTask thisTask, DateTime thisRunDateTime)
		{
			DateTime nextRunDateTime;

			// re-get the task in case its interval has changed (or it has been deleted!) since we invoked it
			ScheduledTask task = this.ScheduledTaskManager.Get(thisTask.Id);

			if (task != null)
			{
				nextRunDateTime = CalculateInterval(thisRunDateTime, task.IntervalType, task.Interval);
				//switch (task.IntervalType)
				//{
				//	case ScheduledTask.Intervals.Minutes:
				//		nextRunDateTime = thisRunDateTime.AddMinutes(task.Interval);
				//		break;
				//	case ScheduledTask.Intervals.Hours:
				//		nextRunDateTime = thisRunDateTime.AddHours(task.Interval);
				//		break;
				//	case ScheduledTask.Intervals.Days:
				//		nextRunDateTime = thisRunDateTime.AddDays(task.Interval);
				//		break;
				//	case ScheduledTask.Intervals.Weeks:
				//		nextRunDateTime = thisRunDateTime.AddDays(task.Interval * 7);
				//		break;
				//	case ScheduledTask.Intervals.Months:
				//		nextRunDateTime = thisRunDateTime.AddMonths(task.Interval);
				//		break;
				//	case ScheduledTask.Intervals.Years:
				//		nextRunDateTime = thisRunDateTime.AddYears(task.Interval);
				//		break;
				//	default:  // case ScheduledTask.IntervalTypes.None:
				//		nextRunDateTime = thisRunDateTime;
				//		break;
				//}

				this.ScheduledTaskManager.ScheduleNextRun(task, nextRunDateTime);
			}
		}

		private DateTime CalculateInterval(DateTime thisRunDateTime, ScheduledTask.Intervals intervalType, int interval)
		{
			switch (intervalType)
			{
				case ScheduledTask.Intervals.Minutes:
					return thisRunDateTime.AddMinutes(interval);
					
				case ScheduledTask.Intervals.Hours:
					return thisRunDateTime.AddHours(interval);
					
				case ScheduledTask.Intervals.Days:
					return thisRunDateTime.AddDays(interval);
					
				case ScheduledTask.Intervals.Weeks:
					return thisRunDateTime.AddDays(interval * 7);
					
				case ScheduledTask.Intervals.Months:
					return thisRunDateTime.AddMonths(interval);
					
				case ScheduledTask.Intervals.Years:
					return thisRunDateTime.AddYears(interval);
					
				default:  // case ScheduledTask.IntervalTypes.None:
					return thisRunDateTime;
					
			}
		}
				
		private void TaskFailed(ScheduledTaskHistory history)
		{
			history.Status = ScheduledTaskProgress.State.Error;
			this.ScheduledTaskManager.SaveHistory(history);
		}

		private void TaskRunning(ScheduledTaskHistory history)
		{
			history.Status = ScheduledTaskProgress.State.Running;
			this.ScheduledTaskManager.SaveHistory(history);
		}

		private void TaskSucceeded(ScheduledTaskHistory history)
		{
			history.Status = ScheduledTaskProgress.State.Succeeded;
			this.ScheduledTaskManager.SaveHistory(history);
		}


		/// <summary>
		/// Start the <see cref="IScheduledTask"/> represented by the specified <see cref="ScheduledTask"/>.
		/// </summary>
		/// <param name="task"></param>
		void StartScheduledTask(ScheduledTask task)
		{
			using (System.Runtime.Loader.AssemblyLoadContext.ContextualReflectionScope scope = Nucleus.Core.Plugins.AssemblyLoader.EnterExtensionContext(task.TypeName))
			{
				ScheduledTaskHistory history = new();
				history.ScheduledTaskId = task.Id;
				history.Server = Environment.MachineName;
				history.StartDate = DateTime.UtcNow;
				history.Status = ScheduledTaskProgress.State.None;
				this.ScheduledTaskManager.SaveHistory(history);

				System.Type serviceType = Type.GetType(task.TypeName);

				if (serviceType == null)
				{					
					this.Logger.LogError("Unable to find type {0}", task.TypeName);
					TaskFailed(history);
					return;
				}

				object service = this.Services.GetService(serviceType);

				if (service == null)
				{
					this.Logger.LogError("Unable to create an instance of {0}", task.TypeName);
					TaskFailed(history);
					return;
				}

				IScheduledTask taskService = service as IScheduledTask;

				if (taskService == null)
				{
					this.Logger.LogError("Unable to create an instance of {0} because it does not implement IScheduledTask", task.TypeName);
					TaskFailed(history);
					return;
				}

				if (task.NextScheduledRun == DateTime.MinValue)
				{
					// when tasks are first created, they have a null date.  If the date is null (DateTime.MinValue), we set the  
					// NextScheduledRun to DateTime.UtcNow so that the re-schedule calculation is based on the current date/time.				
					task.NextScheduledRun = DateTime.UtcNow;
				}
				else if (CalculateInterval(task.NextScheduledRun, task.IntervalType, task.Interval) < DateTime.UtcNow)
				{
					// If the system was stopped and missed the next scheduled run, bump the time up to now
					task.NextScheduledRun = DateTime.UtcNow;
				}

				RunningTask runningTask = this.Queue.Add(task, history);

				TaskRunning(history);
				runningTask.Task = taskService.InvokeAsync(runningTask, runningTask.ProgressCallback, this.CancellationTokenSource.Token);
			}
		}
	}
}
