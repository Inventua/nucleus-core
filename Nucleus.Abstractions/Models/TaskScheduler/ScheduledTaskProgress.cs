using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.TaskScheduler
{
	/// <summary>
	/// Represents running task progress
	/// </summary>		
	public class ScheduledTaskProgress
	{
		/// <summary>
		/// Running task state
		/// </summary>
		public enum State
		{
			/// <summary>
			/// The task has not started.
			/// </summary>
			None=0,
			/// <summary>
			/// The task is running.
			/// </summary>
			Running=1,
			/// <summary>
			/// The task has completed successfully.
			/// </summary>
			Succeeded=2,
			/// <summary>
			/// The task has terminated with an error.
			/// </summary>
			Error=3
		}

		/// <summary>
		/// Percent complete.
		/// </summary>
		/// <remarks>
		/// Use -1 to indicate that the task does not report a percent complete.
		/// </remarks>
		public int Percent { get; set; } = -1;

		/// <summary>
		/// Task state.
		/// </summary>
		public State Status { get; set; } = ScheduledTaskProgress.State.None;

		/// <summary>
		/// Progress message
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Returns a progress:running ScheduledTaskProgress
		/// </summary>
		/// <returns></returns>
		public static ScheduledTaskProgress Running()
		{		
			return new ScheduledTaskProgress() { Status = State.Running };		
		}

		/// <summary>
		/// Returns a progress:success ScheduledTaskProgress
		/// </summary>
		/// <returns></returns>
		public static ScheduledTaskProgress Success()
		{
			return new ScheduledTaskProgress() { Status = State.Succeeded };
		}

		/// <summary>
		/// Returns a progress:error ScheduledTaskProgress
		/// </summary>
		/// <returns></returns>
		public static ScheduledTaskProgress Error()
		{
			return new ScheduledTaskProgress() { Status = State.Error };
		}

	}
}
