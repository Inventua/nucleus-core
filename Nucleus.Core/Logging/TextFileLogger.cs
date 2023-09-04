using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions;
using System.Threading;
using Nucleus.Abstractions.Models.TaskScheduler;
using System.Text.RegularExpressions;
using Nucleus.Abstractions.Models.Configuration;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;

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
    private int MAX_QUEUE_SIZE = 100;

    private string Category { get; }

		private TextFileLoggingProvider Provider { get; }
		private TextFileLoggerOptions Options { get; }
		private IExternalScopeProvider ScopeProvider { get; set; }
		private System.Collections.Concurrent.ConcurrentQueue<string> Queue { get; } = new();
		private static SemaphoreSlim _fileAccessSemaphore { get; } = new(1);

		public TextFileLogger(TextFileLoggingProvider provider, TextFileLoggerOptions options, IExternalScopeProvider scopeProvider, string category)
		{
			this.Provider = provider;
			this.Options = options;
			this.Category = category;
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
				message = FormatMessage(logLevel, eventId, state, exception, formatter);

				if (_fileAccessSemaphore.CurrentCount > 0)
				{
					try
					{
						_fileAccessSemaphore.Wait();
						WriteMessage(message);

						while (this.Queue.TryDequeue(out string queuedMessage))
						{
							WriteMessage(queuedMessage);
						}
					}
          catch (IOException ex)
          {
            // file can be in use if another process is reading it
            Enqueue(message);			
          }
					finally
					{
						_fileAccessSemaphore.Release();
					}
				}
				else
				{
					Enqueue(message);					
				}
			}
		}

    /// <summary>
    /// Queue a message to be written to the log later.
    /// </summary>
    /// <param name="message"></param>
    /// <remarks>
    /// The queue is used when we can't write to the log file immediately because it is already open.  The max queue size check 
    /// is to prevent excessive memory consumption if the log file is inaccessble for a reason other than that is is locked/in use.
    /// </remarks>
    private void Enqueue(string message)
    {
      if (this.Queue.Count < MAX_QUEUE_SIZE)
      {
        this.Queue.Enqueue(message);
      }
    }

		private void WriteMessage(string message)
		{
			// Log message to the standard log file location
			LogMessage(message);

			// If the log scope contains a <RunningTask>, log message to the scheduled task log file location

			// nullState isn't used for anything here.  The IExternalScopeProvider.ForEachScope method requires a TState & state object which allows
			// an object to be passed to the callback but we don't need a state object.
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
			return this.Options.Enabled;
		}

		public IDisposable BeginScope<TState>(TState state) => ScopeProvider?.Push(state);

		private void LogMessage(RunningTask task, string message)
		{
			string logPath = System.IO.Path.Combine(this.Options.Path, ScheduledTask.SCHEDULED_TASKS_LOG_SUBPATH, task.ScheduledTask.GetLogFolderName());
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

			try
			{
				File.AppendAllText(logFilePath, Message + Environment.NewLine);
			}			
			catch(System.UnauthorizedAccessException)
			{
				// stop unauthorized access exception (for logs folder) from stopping the application from running.  Logging
				// providers other than the text file logger will often be enabled/available for troubleshooting, so the log
        // message can be viewed by other means.  Also this is a potentially common error during first-time install
        // which is reported by the installation wizard so we want Nucleus to be able to run so it can show the wizard.
				return;
			}

			if (this.Provider != null)
			{
        // only do this once per day
				if (this.Provider.LastExpiredLogsCheckDate.Date < DateTime.UtcNow.Date)
				{
					this.Provider.LastExpiredLogsCheckDate = DateTime.UtcNow.Date;
          CheckFolder(this.Options.Path);
				}
			}
		}

   

    private void CheckFolder(string path)
    {
      foreach (string logFile in Directory.GetFiles(path, "*.log"))
      {
        // try to avoid deleting log files that don't belong to Nucleus by checking that the filename 
        // is in the expected format.
        Match match = Regex.Match(logFile, LogFileConstants.LOGFILE_REGEX);

        if (match.Success)
        {
          try
          {
            FileInfo objFileData = new(logFile);

            if (objFileData.LastWriteTime < DateTime.Now.Date.Add(-this.Options.LogFileExpiry))
            {
              objFileData.Delete();
            }
          }
          catch (Exception)
          {
            // suppress exception deleting old log file (as this operation is non-critical)
          }
        }
      }

      // check sub-folders.  Scheduled tasks (and theoretically other components) could write logs to sub-folders.
      foreach (string subFolder in Directory.GetDirectories(path))
      {
        CheckFolder(subFolder);
      }
    }
	}
}
