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
	public class ScheduledTaskHistory
	{
		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Link to scheduled task
		/// </summary>
		public Guid ScheduledTaskId { get; set; }
		
		/// <summary>
		/// Task run start date/time
		/// </summary>
		public DateTime StartDate { get; set; }

		/// <summary>
		/// Task completion start date/time
		/// </summary>
		public DateTime? FinishDate { get; set; }

		/// <summary>
		/// Next run date/time
		/// </summary>
		public DateTime? NextScheduledRun { get; set; }

		/// <summary>
		/// Host name of the server which ran the task
		/// </summary>
		public string Server { get; set; }

		/// <summary>
		/// Flag indicating whether the task completed successfully
		/// </summary>
		public ScheduledTaskProgress.State Status { get; set; }

		
	}
}
