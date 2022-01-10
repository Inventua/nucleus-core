using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.DataProviders;
using Microsoft.Extensions.Hosting;

namespace Nucleus.Core
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="ScheduledTask"/>s.
	/// </summary>
	public class ScheduledTaskManager
	{
		private CacheManager CacheManager { get; }
		private DataProviderFactory DataProviderFactory { get; }

		public ScheduledTaskManager(DataProviderFactory dataProviderFactory, CacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
		}

		/// <summary>
		/// Create a new <see cref="ScheduledTask"/> with default values.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This function does not save the new <see cref="ScheduledTask"/> to the database.  Call <see cref="Save(Site, ScheduledTask)"/> to save the role group.
		/// </remarks>
		public ScheduledTask CreateNew()
		{
			return new ScheduledTask();
		}

		// <summary>
		/// Retrieve an existing <see cref="ScheduledTask"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public ScheduledTask Get(Guid id)
		{
			return this.CacheManager.ScheduledTaskCache.Get(id, id =>
			{
				using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
				{
					return provider.GetScheduledTask(id);
				}
			});
		}

		/// <summary>
		/// List all <see cref="ScheduledTask"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public IEnumerable<ScheduledTask> List()
		{
			using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
			{
				return provider.ListScheduledTasks();
			}
		}

		/// <summary>
		/// Returns a list of installed Scheduled task classes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> ListBackgroundServices()
		{
			List<string> results = new();
			foreach (Type type in Plugins.AssemblyLoader.GetTypes<IScheduledTask>())
			{
				results.Add($"{type.FullName},{type.Assembly.GetName().Name}");
			}

			return results;			
		}

		/// <summary>
		/// Update the <see cref="ScheduledTask.NextScheduledRun"/>.
		/// </summary>
		/// <param name="scheduledTask"></param>
		/// <param name="nextRunDateTime"></param>
		public void ScheduleNextRun(ScheduledTask scheduledTask, DateTime nextRunDateTime)
		{
			using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
			{
				provider.ScheduledNextRun(scheduledTask, nextRunDateTime);
				this.CacheManager.ScheduledTaskCache.Remove(scheduledTask.Id);
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="ScheduledTaskHistory"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="scheduledTask"></param>
		public void SaveHistory(ScheduledTaskHistory history)
		{
			using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
			{
				provider.SaveScheduledTaskHistory(history);				
			}
		}

		/// <summary>
		/// Remove old history records.
		/// </summary>
		/// <param name="scheduledTask"></param>
		public void TruncateHistory(ScheduledTask scheduledTask)
		{
			using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
			{
				foreach (ScheduledTaskHistory history in provider.ListScheduledTaskHistory(scheduledTask.Id).OrderByDescending(history=>history.StartDate).Skip(scheduledTask.KeepHistoryCount))
				{
					provider.DeleteScheduledTaskHistory(history);
				}
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="ScheduledTaskHistory"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="scheduledTask"></param>
		public List<ScheduledTaskHistory> ListHistory(ScheduledTask task)
		{
			using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
			{
				return provider.ListScheduledTaskHistory(task.Id).OrderByDescending(history=>history.StartDate).ToList();
			}
		}


		/// <summary>
		/// Create or update the specified <see cref="ScheduledTask"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="scheduledTask"></param>
		public void Save(ScheduledTask scheduledTask)
		{
			using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
			{				
				provider.SaveScheduledTask( scheduledTask);
				this.CacheManager.ScheduledTaskCache.Remove(scheduledTask.Id);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="ScheduledTask"/> from the database.
		/// </summary>
		/// <param name="scheduledTask"></param>
		public void Delete(ScheduledTask scheduledTask)
		{
			using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
			{
				provider.DeleteScheduledTask(scheduledTask);
				this.CacheManager.ScheduledTaskCache.Remove(scheduledTask.Id);
			}
		}

	}
}
