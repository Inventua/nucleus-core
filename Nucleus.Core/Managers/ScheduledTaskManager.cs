using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="ScheduledTask"/>s.
	/// </summary>
	public class ScheduledTaskManager : IScheduledTaskManager
	{
		private ICacheManager CacheManager { get; }
		private IDataProviderFactory DataProviderFactory { get; }

		public ScheduledTaskManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager)
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
		public Task<ScheduledTask> CreateNew()
		{
			return Task.FromResult(new ScheduledTask());
		}

		// <summary>
		/// Retrieve an existing <see cref="ScheduledTask"/> from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<ScheduledTask> Get(Guid id)
		{
			return await this.CacheManager.ScheduledTaskCache().GetAsync(id, async id =>
			{
				using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
				{
					return await provider.GetScheduledTask(id);
				}
			});
		}

		/// <summary>
		/// List all <see cref="ScheduledTask"/>s for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public async Task<IEnumerable<ScheduledTask>> List()
		{
			using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
			{
				return await provider.ListScheduledTasks();
			}
		}

		/// <summary>
		/// Returns a list of installed Scheduled task classes.
		/// </summary>
		/// <returns></returns>
		public Task<IEnumerable<string>> ListBackgroundServices()
		{
			List<string> results = new();
			foreach (Type type in Plugins.AssemblyLoader.GetTypes<IScheduledTask>())
			{
				results.Add($"{type.FullName},{type.Assembly.GetName().Name}");
			}

			return Task.FromResult(results as IEnumerable<string>);			
		}

		/// <summary>
		/// Update the <see cref="ScheduledTask.NextScheduledRun"/>.
		/// </summary>
		/// <param name="scheduledTask"></param>
		/// <param name="nextRunDateTime"></param>
		public async Task ScheduleNextRun(ScheduledTask scheduledTask, DateTime nextRunDateTime)
		{
			using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
			{
				await provider.ScheduleNextRun(scheduledTask, nextRunDateTime);
				this.CacheManager.ScheduledTaskCache().Remove(scheduledTask.Id);
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="ScheduledTaskHistory"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="scheduledTask"></param>
		public async Task SaveHistory(ScheduledTaskHistory history)
		{
			using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
			{
				await provider.SaveScheduledTaskHistory(history);				
			}
		}

		/// <summary>
		/// Remove old history records.
		/// </summary>
		/// <param name="scheduledTask"></param>
		public async Task TruncateHistory(ScheduledTask scheduledTask)
		{
			using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
			{
				foreach (ScheduledTaskHistory history in (await provider.ListScheduledTaskHistory(scheduledTask.Id)).OrderByDescending(history=>history.StartDate).Skip(scheduledTask.KeepHistoryCount))
				{
					await provider .DeleteScheduledTaskHistory(history);
				}
			}
		}

		/// <summary>
		/// Create or update the specified <see cref="ScheduledTaskHistory"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="scheduledTask"></param>
		public async Task<List<ScheduledTaskHistory>> ListHistory(ScheduledTask task)
		{
			using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
			{
				return (await provider.ListScheduledTaskHistory(task.Id)).OrderByDescending(history=>history.StartDate).ToList();
			}
		}
		 

		/// <summary>
		/// Create or update the specified <see cref="ScheduledTask"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="scheduledTask"></param>
		public async Task Save(ScheduledTask scheduledTask)
		{
			using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
			{				
				await provider.SaveScheduledTask( scheduledTask);
				this.CacheManager.ScheduledTaskCache().Remove(scheduledTask.Id);
			}
		}

		/// <summary>
		/// Delete the specified <see cref="ScheduledTask"/> from the database.
		/// </summary>
		/// <param name="scheduledTask"></param>
		public async Task Delete(ScheduledTask scheduledTask)
		{
			using (IScheduledTaskDataProvider provider = this.DataProviderFactory.CreateProvider<IScheduledTaskDataProvider>())
			{
				await provider .DeleteScheduledTask(scheduledTask);
				this.CacheManager.ScheduledTaskCache().Remove(scheduledTask.Id);
			}
		}

	}
}
