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

namespace Nucleus.Core.ScheduledTasks
{
	public class CollectCacheScheduledTask : IScheduledTask
	{
		private CacheManager CacheManager { get; }

		public CollectCacheScheduledTask(CacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
		}

		public Task InvokeAsync(RunningTask task, IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken)
		{
			return Task.Run(CollectCache);
		}
		
		private void CollectCache()
		{
			this.CacheManager.PageCache.Collect();
			this.CacheManager.MailTemplateCache.Collect();
			this.CacheManager.PageModuleCache.Collect();
			this.CacheManager.RoleCache.Collect();

			this.CacheManager.RoleGroupCache.Collect();
			this.CacheManager.ScheduledTaskCache.Collect();
			this.CacheManager.SiteCache.Collect();
			this.CacheManager.SiteDetectCache.Collect();

			this.CacheManager.UserCache.Collect();
		}

	}
}
