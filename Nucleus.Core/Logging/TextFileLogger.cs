using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using Nucleus.Abstractions;
using System.Threading;
using Nucleus.Abstractions.Models.TaskScheduler;
using System.Collections.Generic;

namespace Nucleus.Core.Logging
{
	/// <summary>
	/// The Text file logger writes log messages to files in the folder specified by Options.Path.  
	/// </summary>
	/// <remarks>
	/// A Text file logger is created by the <see cref="TextFileLoggingProvider"/>.  Text file logs are
	/// automatically deleted after 7 days.  Logging is configured in appSettings.json.
	/// </remarks>
	public class TextFileLogger : ILogger
	{
		private string Category { get; }
		private const int LOG_FILE_RETENTION_DAYS = 7;

		//private readonly static object syncObj = new();
		private TextFileLoggingProvider Provider { get; }
		private TextFileLoggerOptions Options { get; }
		private IExternalScopeProvider ScopeProvider { get; set; }
		private System.Collections.Concurrent.ConcurrentQueue<string> Queue { get; } = new();
		private SemaphoreSlim FileAccessSemaphore { get; } = new(1);
		
		public TextFileLogger(TextFileLoggingProvider Provider, TextFileLoggerOptions Options, IExternalScopeProvider scopeProvider, string Category)
		{
			this.Provider = Provider;
			this.Options = Options;
			this.Category = Category;
			this.ScopeProvider = scopeProvider;
		}

		public TextFileLogger(string Category)
		{
			this.Category = Category;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			string message = null;

			if (IsEnabled(logLevel))
			{
				try
				{
					message = FormatMessage(logLevel, eventId, state, exception, formatter);

					try
					{
						if (this.FileAccessSemaphore.CurrentCount > 0)
						{
							this.FileAccessSemaphore.Wait();

							string queuedMessage;

							while (this.Queue.TryDequeue(out queuedMessage))
							{
								WriteMessage(queuedMessage);
							}
						}
						else
						{
							this.Queue.Enqueue(message);
						}
					}
					finally
					{
						this.FileAccessSemaphore.Release();
					}
				}
				catch (Exception)
				{
				}
			}
		}

		private void WriteMessage(string message)
		{
			// Log message to the standard log file location
			LogMessage(message);

			// If the log scope contains a <RunningTask>, log message to the scheduled task log file location

			// nullState isn't used for anything here.  The IExternalScopeProvider.ForEachScope method requires a TState & state object which allows
			// an object to be passed to the callback but we don't need to do that.
			Object nullState = null;

			this.ScopeProvider.ForEachScope<Object>((scope, state) =>
			{
				if (scope as RunningTask != null)
				{
					LogMessage(scope as RunningTask, message);
				}
			}, nullState);
		}

		private string FormatMessage<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			if (exception != null)
			{
				return $"{DateTime.UtcNow:dd-MMM-yyyy HH:mm:ss.fffffff},{logLevel},{this.Category},{formatter(state, exception)} {exception.Message}: {exception.ToString()}".Replace(Environment.NewLine, "|").Replace("\n", "");
			}
			else
			{
				return $"{DateTime.UtcNow:dd-MMM-yyyy HH:mm:ss.fffffff},{logLevel},{this.Category},{formatter(state, exception)}".Replace(Environment.NewLine, "|");
			}
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return true;
		}

		public IDisposable BeginScope<TState>(TState state) => ScopeProvider?.Push(state);

		private void LogMessage(RunningTask task, string message)
		{
			string logPath = System.IO.Path.Combine(this.Options.Path, ScheduledTask.SCHEDULED_TASKS_LOG_SUBPATH, task.ScheduledTask.Name);
			string logFileName = $"{task.StartDate.ToString(LogFileConstants.DATETIME_FILENAME_FORMAT)}_{Environment.MachineName}.log";

			LogMessage(message, logPath, logFileName);
		}

		private void LogMessage(string message)
		{
			string fileName = $"{DateTime.UtcNow.ToString(LogFileConstants.DATE_FILENAME_FORMAT)}_{Environment.MachineName}.log";
			LogMessage(message, this.Options.Path, fileName);
		}

		private void LogMessage(string Message, string path, string fileName)
		{
			string logFilePath = Path.Combine(path, fileName);

			if (!System.IO.Directory.Exists(path))
			{
				System.IO.Directory.CreateDirectory(path);
			}
			File.AppendAllText(logFilePath, Message + Environment.NewLine);

			if (this.Provider != null)
			{
				if (this.Provider.LastExpiredDocumentsCheckDate.Date < DateTime.UtcNow.Date)
				{
					this.Provider.LastExpiredDocumentsCheckDate = DateTime.UtcNow.Date;

					foreach (string strOldLogFile in Directory.GetFiles(this.Options.Path, "*.log"))
					{
						// try to avoid deleting log files that don't belong to Nucleus by checking that the filename 
						// is in the expected format.
						System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(strOldLogFile, LogFileConstants.LOGFILE_REGEX);

						if (match.Success)
						{
							try
							{
								FileInfo objFileData = new(strOldLogFile);

								if (objFileData.LastWriteTime < DateTime.Now.Date.AddDays(-LOG_FILE_RETENTION_DAYS))
								{
									objFileData.Delete();
								}
							}
							catch (Exception)
							{
								// suppress exception deleting old logs (non-critical)
							}
						}
					}
				}
			}
		}
	}
}
