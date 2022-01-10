using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.TaskScheduler;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;

namespace Nucleus.Core.Logging
{
  public static class ScheduledTaskLoggerExtensions
  {
		public static void LogInformation(this ILogger logger, RunningTask task, string message, params object[] args)
		{
			logger.Log(LogLevel.Information, new EventId(0), new ScheduledTaskLogInfo(task, message, args), null, (state, exception) => state.ToString());
		}

	}

	public class ScheduledTaskLogInfo
	{
		private RunningTask TaskInfo { get; }
		private string Message { get; }
		private object[] Args { get; }

		public ScheduledTaskLogInfo(RunningTask taskInfo, string message, params object[] args)
		{
			this.TaskInfo = taskInfo;
			this.Message = message;
			this.Args = args;
		}

		public string LogPath
		{
			get
			{
				return System.IO.Path.Combine(Constants.SCHEDULED_TASKS_LOG_SUBPATH, this.TaskInfo.ScheduledTask.Name);
			}
		}

		public string LogFile
		{
			get
			{
				return $"{this.TaskInfo.StartDate.ToString(Constants.DATETIME_FILENAME_FORMAT)}.log";
			}
		}

		public override string ToString()
		{
			return String.Format(this.Message, this.Args);
		}
	}
}