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
	public class ScheduledTask
	{

		public enum Intervals
		{
			None = 0,
			Minutes = 1,
			Hours = 2,
			Days = 3,
			Weeks = 4,
			Months = 5,
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
		public Boolean Enabled { get; set; }

		/// <summary>
		/// The date/time that the task will next run.
		/// </summary>
		public DateTime NextScheduledRun { get; set; }

		/// <summary>
		/// Specifies the number of history records to keep.
		/// </summary>
		public int KeepHistoryCount { get; set; }
	}
}
