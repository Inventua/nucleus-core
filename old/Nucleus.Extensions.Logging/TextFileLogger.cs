//using System;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using System.ComponentModel;

//namespace Nucleus.Extensions.Logging
//{
//	/// <summary>
//	/// The Text file logger writes log messages to files in %CommonApplicationData%\Inventua\OsaAppCore\Logs.  
//	/// </summary>
//	/// <remarks>
//	/// A Text file logger is created by the <see cref="TextFileLoggingProvider"/>.  Text file logs are
//	/// automatically deleted after 7 days.  Logging is configured in /config/appSettings.json.
//	/// </remarks>
//	public class TextFileLogger : ILogger
//	{
//		private string Category { get; }
//		private const int LOG_FILE_RETENTION_DAYS = 7;
		

//		private static object SyncObj { get; } = new object();
//		private TextFileLoggingProvider Provider { get; }
//		private TextFileLoggerOptions Options { get; }


//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		public TextFileLogger(TextFileLoggingProvider Provider, TextFileLoggerOptions Options, string Category)
//		{
//			this.Provider = Provider;
//			this.Options = Options;
//			this.Category = Category;
//		}

//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		public TextFileLogger(string Category)
//		{
//			this.Category = Category;
//		}

//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
//		{
//			string message = null;

//			if (IsEnabled(logLevel))
//			{
//				try
//				{
//					message = FormatMessage(logLevel, eventId, state, exception, formatter);
//					LogMessage(message);
//				}
//				catch (Exception)
//				{
//				}
//			}			
//		}

//		private string FormatMessage<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
//		{
//			string strEventType;

//			if (eventId.Name == null)
//				strEventType = "";
//			else
//				strEventType = eventId.Name;

//			if (exception != null)
//				return
//				(
//					string.Format("{0:dd-MMM-yyyy HH:mm:ss.fffffff} {1,-12} {2}", DateTime.Now, "[" + logLevel.ToString() + "]", this.Category) + Environment.NewLine + string.Format("{0,-28} {1}", " ", formatter(state, exception) + " " + exception.Message + Environment.NewLine + exception.ToString())
//				);
//			else
//				return
//				(
//					string.Format("{0:dd-MMM-yyyy HH:mm:ss.fffffff} {1,-12} {2} {3}", DateTime.Now, "[" + logLevel.ToString() + "]", this.Category, strEventType) + Environment.NewLine + string.Format("{0,-28} {1}", " ", formatter(state, exception))
//				);
//		}

//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		public bool IsEnabled(LogLevel logLevel)
//		{
//			return true;
//		}

//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		public IDisposable BeginScope<TState>(TState state)
//		{			
//			return null;
//		}

		

//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		private void LogMessage(string Message)
//		{
//			// TODO: Control log path from an options class?
//			//string logPath = ApplicationFolders.GetDataFolder("Logs");
//			string fileName = $"{DateTime.Now:dd-MMM-yyyy}.log";
//			string logFile = Path.Combine(this.Options.Path, fileName);

//			LogMessage(Message, logFile);

//			if (this.Provider != null)
//			{
//				if (this.Provider.LastExpiredDocumentsCheckDate.Date < DateTime.Now.Date)
//				{
//					this.Provider.LastExpiredDocumentsCheckDate = DateTime.Now.Date;

//					foreach (string strOldLogFile in Directory.GetFiles(this.Options.Path))
//					{
//						try
//						{
//							FileInfo objFileData = new FileInfo(strOldLogFile);

//							if (objFileData.LastWriteTime < DateTime.Now.Date.AddDays(-LOG_FILE_RETENTION_DAYS))
//								objFileData.Delete();
//						}
//						catch (Exception)
//						{
//							// suppress exception deleting old logs (non-critical)
//						}
//					}
//				}
//			}
//		}

//		private void LogMessage(string Message, string logFile)
//		{
//			lock (SyncObj)
//			{
//				File.AppendAllText(logFile, Message + Environment.NewLine);
//			}
//		}
//	}
//}
