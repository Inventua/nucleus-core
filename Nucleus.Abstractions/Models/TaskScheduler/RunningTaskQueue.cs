using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Nucleus.Abstractions.Models.TaskScheduler
{
	/// <summary>
	/// Represents a collection of running tasks
	/// </summary>
	public class RunningTaskQueue 
	{
		private ConcurrentDictionary<Guid, RunningTask> Queue { get; } = new();

		/// <summary>
		/// Returns whether the scheduled task is in the queue.
		/// </summary>
		/// <param name="task"></param>
		/// <returns></returns>
		public Boolean Contains(ScheduledTask task)
		{
			return this.Queue.ContainsKey(task.Id);
		}

		/// <summary>
		/// Create a new RunningTask object, add it to the queue and return it.
		/// </summary>
		/// <param name="task"></param>
		/// <param name="history"></param>
		/// <returns></returns>
		public RunningTask Add(ScheduledTask task, ScheduledTaskHistory history)
		{
			RunningTask runningTask = new(task, history);
		
			this.Queue.TryAdd(task.Id, runningTask);

			return runningTask;
		}

		/// <summary>
		/// Removes a scheduled task from the queue.
		/// </summary>
		/// <param name="task"></param>
		/// <returns></returns>
		public Boolean Remove(ScheduledTask task)
		{
			return this.Queue.TryRemove(task.Id, out _);
		}

		/// <summary>
		/// Returns a list of running tasks.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<RunningTask> ToList()
		{
			return this.Queue.Values.ToArray();
		}
	}
}
