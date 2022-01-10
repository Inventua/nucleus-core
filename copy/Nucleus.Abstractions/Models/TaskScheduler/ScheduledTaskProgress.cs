using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.TaskScheduler
{
	public class ScheduledTaskProgress
	{
		/// <summary>
		/// Represents running task progress
		/// </summary>
		public enum State
		{
			None=0,
			Running=1,
			Succeeded=2,
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

		public static ScheduledTaskProgress Running()
		{		
			return new ScheduledTaskProgress() { Status = State.Running };		
		}

		public static ScheduledTaskProgress Success()
		{
			return new ScheduledTaskProgress() { Status = State.Succeeded };
		}

		public static ScheduledTaskProgress Error()
		{
			return new ScheduledTaskProgress() { Status = State.Error };
		}

	}
}
