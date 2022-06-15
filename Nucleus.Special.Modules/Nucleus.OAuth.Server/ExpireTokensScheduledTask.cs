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

namespace Nucleus.OAuth.Server
{
	[System.ComponentModel.DisplayName("Nucleus OAuth Server: Clean up expired tokens")]
	public class ExpireTokensScheduledTask : IScheduledTask
	{
		private ClientAppTokenManager ClientAppTokenManager { get; }

		public ExpireTokensScheduledTask(ClientAppTokenManager clientAppTokenManager)
		{
			this.ClientAppTokenManager = clientAppTokenManager;
		}

		public Task InvokeAsync(RunningTask task, IProgress<ScheduledTaskProgress> progress, CancellationToken cancellationToken)
		{
			return Task.Run(()=> ExpireTokens(progress));
		}
		
		private async Task ExpireTokens(IProgress<ScheduledTaskProgress> progress)
		{
			await this.ClientAppTokenManager.ExpireTokens();
			
			progress.Report(new ScheduledTaskProgress() { Status = ScheduledTaskProgress.State.Succeeded });
			
		}

	}
}
