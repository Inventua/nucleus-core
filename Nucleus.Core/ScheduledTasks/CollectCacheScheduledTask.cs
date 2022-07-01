using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.TaskScheduler;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Core.ScheduledTasks
{
	[System.ComponentModel.DisplayName("Nucleus Core: Cache Cleanup")]
	public class CollectCacheScheduledTask : IScheduledTask
	{
		private ICacheManager CacheManager { get; }

		public CollectCacheScheduledTask(ICacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
		}

		public Task InvokeAsync(RunningTask task, IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken)
		{
			//return Task.Run(()=> CollectCache(progress));
			return Task.Run(() => CollectCache(progress));
		}
		
		private void CollectCache(IProgress<ScheduledTaskProgress> progress)
		{
			this.CacheManager.Collect();
			progress.Report(new ScheduledTaskProgress() { Status = ScheduledTaskProgress.State.Succeeded });			
		}

	}
}
