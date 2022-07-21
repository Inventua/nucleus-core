using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Defines the interface for the scheduled task manager.
	/// </summary>
	/// <remarks>
	/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
	/// </remarks>
	public interface IScheduledTaskManager
	{
		/// <summary>
		/// Create a new <see cref="ScheduledTask"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="ScheduledTask"/> to the database.  Call <see cref="Save(ScheduledTask)"/> to save the role group.
		/// </remarks>
		public Task<ScheduledTask> CreateNew();

		/// <summary>
		/// Retrieve an existing <see cref="ScheduledTask"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<ScheduledTask> Get(Guid id);

		/// <summary>
		/// List all <see cref="ScheduledTask"/>s for the specified site.
		/// </summary>
		/// <returns></returns>
		public Task<IEnumerable<ScheduledTask>> List();

		/// <summary>
		/// List <see cref="ScheduledTask"/>s for the specified site.
		/// </summary>
		/// <returns></returns>
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<ScheduledTask>> List(Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);

		/// <summary>
		/// Returns a list of installed Scheduled task classes.
		/// </summary>
		/// <returns></returns>
		public Task<IEnumerable<System.Type>> ListBackgroundServices();

		///// <summary>
		///// Update the <see cref="ScheduledTask.NextScheduledRun"/>.
		///// </summary>
		///// <param name="scheduledTask"></param>
		///// <param name="nextRunDateTime"></param>
		//public Task ScheduleNextRun(ScheduledTask scheduledTask, DateTime nextRunDateTime);

		/// <summary>
		/// Create or update the specified <see cref="ScheduledTaskHistory"/>.
		/// </summary>
		/// <param name="history"></param>
		public Task SaveHistory(ScheduledTaskHistory history);

		/// <summary>
		/// Remove old history records.
		/// </summary>
		/// <param name="scheduledTask"></param>
		public Task TruncateHistory(ScheduledTask scheduledTask);

		/// <summary>
		/// Create or update the specified <see cref="ScheduledTaskHistory"/>.
		/// </summary>
		/// <param name="task"></param>
		public Task<List<ScheduledTaskHistory>> ListHistory(ScheduledTask task);

		/// <summary>
		/// Get the most recent scheduled task history for the specified server.
		/// </summary>
		/// <param name="scheduledTask"></param>
		/// <param name="server"></param>
		/// <returns></returns>
		/// <remarks>
		/// Specify NULL for the server name to ignore the server name.
		/// </remarks>
		public Task<ScheduledTaskHistory> GetMostRecentHistory(ScheduledTask scheduledTask, string server);

		/// <summary>
		/// Create or update the specified <see cref="ScheduledTask"/>.
		/// </summary>
		/// <param name="scheduledTask"></param>
		public Task Save(ScheduledTask scheduledTask);

		/// <summary>
		/// Delete the specified <see cref="ScheduledTask"/> from the database.
		/// </summary>
		/// <param name="scheduledTask"></param>
		public Task Delete(ScheduledTask scheduledTask);

	}
}
