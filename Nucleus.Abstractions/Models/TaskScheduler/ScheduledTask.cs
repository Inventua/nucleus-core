using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Abstractions.Models.TaskScheduler
{
	/// <summary>
	/// Represents the settings for a scheduled task.
	/// </summary>
	public class ScheduledTask : ModelBase
	{
		/// <summary>
		/// Name of the sub-folder which stores scheduled task logs
		/// </summary>
		public static string SCHEDULED_TASKS_LOG_SUBPATH = "Scheduled Tasks";

		/// <summary>
		/// Internval types
		/// </summary>
		public enum Intervals
		{
			/// <summary>
			/// None (do not execute the task)
			/// </summary>
			None = 0,
			/// <summary>
			/// Minutes
			/// </summary>
			Minutes = 1,
			/// <summary>
			/// Hours
			/// </summary>
			Hours = 2,
			/// <summary>
			/// Days
			/// </summary>
			Days = 3,
			/// <summary>
			/// Weeks
			/// </summary>
			Weeks = 4,
			/// <summary>
			/// Months
			/// </summary>
			Months = 5,
			/// <summary>
			/// Years
			/// </summary>
			Years = 6
		}

		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Task friendly name
		/// </summary>
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// Assembly-qualified class name for the class which implements the task.  The class must implement Nucleus.Abstractions.IScheduledTask
		/// </summary>
		public string TypeName { get; set; }

		/// <summary>
		/// Task execution interval type
		/// </summary>
		public Intervals IntervalType { get; set; }

		/// <summary>
		/// Task execution interval
		/// </summary>
		public int Interval { get; set; }

		/// <summary>
		/// Specifies whether the task is enabled.  Disabled tasks will not run.
		/// </summary>
		public Boolean Enabled { get; set; } = true;

		/// <summary>
		/// The date/time that the task will next run.
		/// </summary>
		public DateTime? NextScheduledRun { get; set; }

		/// <summary>
		/// Specifies the number of history records to keep.
		/// </summary>
		public int KeepHistoryCount { get; set; }


	}
}
