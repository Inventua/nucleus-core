using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.TaskScheduler;

namespace Nucleus.Core.DataProviders
{
  /// <summary>
  /// Provides create, read, update and delete functionality for the <see cref="ScheduledTask"/> class.
  /// </summary>
  internal interface IScheduledTaskDataProvider : IDisposable//, IDataProvider<IScheduledTaskDataProvider>
	{
		abstract Task<ScheduledTask> GetScheduledTaskByTypeName(string typeName);
		abstract Task SaveScheduledTask(ScheduledTask scheduledTask);
		//abstract Task ScheduleNextRun(ScheduledTask scheduledTask, DateTime nextRunDateTime);

		abstract Task<ScheduledTaskHistory> GetMostRecentHistory(ScheduledTask scheduledTask, string server);

		abstract Task<ScheduledTask> GetScheduledTask(Guid scheduledTaskId);
		abstract Task<List<ScheduledTask>> ListScheduledTasks();
		abstract Task<Nucleus.Abstractions.Models.Paging.PagedResult<ScheduledTask>> ListScheduledTasks(Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings);
		abstract Task DeleteScheduledTask(ScheduledTask scheduledTask);
		abstract Task SaveScheduledTaskHistory(ScheduledTaskHistory history);
		abstract Task<List<ScheduledTaskHistory>> ListScheduledTaskHistory(Guid scheduledTaskId);
		abstract Task DeleteScheduledTaskHistory(ScheduledTaskHistory history);
    abstract Task TruncateScheduledTaskHistory(Guid scheduledTaskId, int keepHistoryCount);
   
  }
}

