using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;

namespace Nucleus.Core.DataProviders
{
	/// <summary>
	/// Provides create, read, update and delete functionality for the <see cref="ScheduledTask"/> class.
	/// </summary>
	internal interface IScheduledTaskDataProvider : IDisposable, Abstractions.IDataProvider
	{
		abstract void SaveScheduledTask(ScheduledTask scheduledTask);
		abstract void ScheduledNextRun(ScheduledTask scheduledTask, DateTime nextRunDateTime);
		abstract ScheduledTask GetScheduledTask(Guid scheduledTaskId);
		abstract IEnumerable<ScheduledTask> ListScheduledTasks();
		abstract void DeleteScheduledTask(ScheduledTask scheduledTask);
		abstract void SaveScheduledTaskHistory(ScheduledTaskHistory history);
		abstract List<ScheduledTaskHistory> ListScheduledTaskHistory(Guid scheduledTaskId);
		abstract void DeleteScheduledTaskHistory(ScheduledTaskHistory history);

	}
}
