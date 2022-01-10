using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Data.Common;

namespace Nucleus.Core.DataProviders
{
	/// <summary>
	/// Provides create, read, update and delete functionality for the <see cref="ScheduledTask"/> class.
	/// </summary>
	internal interface IScheduledTaskDataProvider : IDisposable//, IDataProvider<IScheduledTaskDataProvider>
	{
		abstract Task<ScheduledTask> GetScheduledTaskByTypeName(string typeName);
		abstract Task SaveScheduledTask(ScheduledTask scheduledTask);
		abstract Task ScheduleNextRun(ScheduledTask scheduledTask, DateTime nextRunDateTime);
		abstract Task<ScheduledTask> GetScheduledTask(Guid scheduledTaskId);
		abstract Task<List<ScheduledTask>> ListScheduledTasks();
		abstract Task DeleteScheduledTask(ScheduledTask scheduledTask);
		abstract Task SaveScheduledTaskHistory(ScheduledTaskHistory history);
		abstract Task<List<ScheduledTaskHistory>> ListScheduledTaskHistory(Guid scheduledTaskId);
		abstract Task DeleteScheduledTaskHistory(ScheduledTaskHistory history);

	}
}
