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
	public class CollectCacheScheduledTask : IScheduledTask
	{
		private ICacheManager CacheManager { get; }

		public CollectCacheScheduledTask(ICacheManager cacheManager)
		{
			this.CacheManager = cacheManager;
		}

		public Task InvokeAsync(RunningTask task, IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken)
		{
			return Task.Run(()=> CollectCache(progress));
		}
		
		private void CollectCache(IProgress<ScheduledTaskProgress> progress)
		{
			this.CacheManager.Collect();
			
			//this.CacheManager.PageCache().Collect();
			//this.CacheManager.PageMenuCache().Collect();
			//this.CacheManager.MailTemplateCache().Collect();
			//this.CacheManager.PageModuleCache().Collect();

			//this.CacheManager.RoleCache().Collect();
			//this.CacheManager.RoleGroupCache().Collect();
			//this.CacheManager.ScheduledTaskCache().Collect();
			//this.CacheManager.SiteCache().Collect();

			//this.CacheManager.SiteGroupCache().Collect();
			//this.CacheManager.UserCache().Collect();
			//this.CacheManager.FolderCache().Collect();
			//this.CacheManager.ListCache().Collect();

			//this.CacheManager.ContentCache().Collect();

			progress.Report(new ScheduledTaskProgress() { Status = ScheduledTaskProgress.State.Succeeded });
			
		}

	}
}
