using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using Nucleus.Abstractions;
using System.Threading;
using Nucleus.Abstractions.Models.TaskScheduler;

namespace Nucleus.Core.Logging
{
	/// <summary>
	/// The Text file logger writes log messages to files in %CommonApplicationData%\Nucleus\Logs.  
	/// </summary>
	/// <remarks>
	/// A Text file logger is created by the <see cref="TextFileLoggingProvider"/>.  Text file logs are
	/// automatically deleted after 7 days.  Logging is configured in appSettings.json.
	/// </remarks>
	public class TextFileLogger : ILogger
	{
		private string Category { get; }
		private const int LOG_FILE_RETENTION_DAYS = 7;
		
		private readonly static object syncObj = new();
		private TextFileLoggingProvider Provider { get; }
		private TextFileLoggerOptions Options { get; }
		private static AsyncLocal<System.Collections.Stack> ScopeStack { get; } = new();

		public TextFileLogger(TextFileLoggingProvider Provider, TextFileLoggerOptions Options, string Category)
		{
			this.Provider = Provider;
			this.Options = Options;
			this.Category = Category;			
		}

		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
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
					LogMessage(message);
										
					if (ScopeStack.Value?.Count > 0 && ScopeStack.Value?.Peek() is RunningTask)
					{
						RunningTask logInfo = ScopeStack.Value?.Peek() as RunningTask;
						// Log message again to the scheduled task log
						if (logInfo != null)
						{
							LogMessage(logInfo, message);
						}
					}
				}
				catch (Exception)
				{
				}
			}			
		}

		private string FormatMessage<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			if (exception != null)
			{
				return $"{DateTime.UtcNow:dd-MMM-yyyy HH:mm:ss.fffffff},{logLevel},{this.Category},{formatter(state, exception)} {exception.Message}: {exception.ToString()}".Replace(Environment.NewLine, "|");
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

		public IDisposable BeginScope<TState>(TState state)
		{
			if (ScopeStack.Value == null)
			{
				ScopeStack.Value = new();
			}
			ScopeStack.Value.Push(state);
			return new DisposableScope();
		}

		private class DisposableScope : IDisposable
		{
			Boolean _disposed = false;

			public void Dispose()
			{
				// Dispose of unmanaged resources.
				Dispose(true);
				// Suppress finalization.
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				if (_disposed)
				{
					return;
				}

				if (disposing)
				{
					// dispose managed state (managed objects).
					ScopeStack.Value.Pop();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				_disposed = true;
			}

			//public void Dispose(Boolean disposing)
			//{
			//	ScopeStack.Value.Pop();
			//}
		}

		private void LogMessage(RunningTask task, string message)
		{
			string logPath = System.IO.Path.Combine(this.Options.Path, ScheduledTask.SCHEDULED_TASKS_LOG_SUBPATH, task.ScheduledTask.Name);
			string logFileName = $"{task.StartDate.ToString(LogFileConstants.DATETIME_FILENAME_FORMAT)}_{Environment.MachineName}.log";

			LogMessage (message, logPath, logFileName);
		}

		private void LogMessage(string message)
		{
			string fileName = $"{DateTime.UtcNow.ToString(LogFileConstants.DATE_FILENAME_FORMAT)}_{Environment.MachineName}.log";
			LogMessage(message, this.Options.Path, fileName);			
		}

		private void LogMessage(string Message, string path, string fileName)
		{
			string logFilePath = Path.Combine(path, fileName);

			lock (syncObj)
			{
				if (!System.IO.Directory.Exists(path))
				{
					System.IO.Directory.CreateDirectory(path);
				}
				File.AppendAllText(logFilePath, Message + Environment.NewLine);
			}

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
