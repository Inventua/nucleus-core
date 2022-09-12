using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.Logging; 

namespace Nucleus.Web.ViewModels.Admin
{	
	public class SystemIndex
	{
		public string Server { get; set; }
		public string Product { get; set; }

		public string Version { get; set; }

		public string Company { get; set; }

		public string Copyright { get; set; }

		public string Framework { get; set; }

		public string OperatingSystem { get; set; }

		public string OperatingSystemUser { get; set; }

		public DateTime StartTime { get; set; }
		public string Uptime { get; set; }
		
		public LogSettingsViewModel LogSettings { get; set; } = new();

		public string Configuration { get; set; }

		public IEnumerable<Nucleus.Abstractions.Models.TaskScheduler.RunningTask> RunningTasks { get; set; }

		public IEnumerable<DatabaseConnection> DatabaseConnections { get; set; }
		public long UsersOnline { get; set; }

		//public Dictionary<string, string> WebServerInformation { get; set; } = new();

		public Dictionary<string, string> LoggingSettingsConfiguration { get; set; } = new();

		public SystemIndex()
		{
		}

		public class LogSettingsViewModel
		{
			public string ControllerName { get; set; }
			public Boolean LogSortDescending { get; set; }
			public string LogFilterTerm { get; set; }
			public Boolean LogIncludeInformation { get; set; } = true;
			public Boolean LogIncludeWarning { get; set; } = true;
			public Boolean LogIncludeTrace { get; set; } = true;
			public Boolean LogIncludeError { get; set; } = true;

			public List<KeyValuePair<Boolean, string>> LogSortOrders { get; } = new() { new(true, "Sort Descending"), new(false, "Sort Ascending") };

			public List<Shared.LogFileInfo> LogFiles { get; set; }

			public string LogFile { get; set; }

			public Nucleus.Abstractions.Models.Paging.PagedResult<LogEntry> LogContent { get; set; } = new() { PageSize = 100, PageSizes = new List<int>() { 100, 250, 500 } };

			public string LogMessage { get; set; }
		}

		public class LogEntry
		{
			public DateTime Date { get; set; }
			public string Level { get; set; }
			public string Category { get; set; }
			public string Message { get; set; }
			public Boolean IsValid { get; set; } = true;
			private string Raw { get; set; }

			public LogEntry(string input)
			{
				this.Raw=input;	

				string[] parts = input.Split(',');

				if (parts.Length >= 4)
				{
					if (DateTime.TryParse(parts[0], out DateTime result))
					{
						this.Date = result;
					}
					this.Level = parts[1];
					this.Category = parts[2];

					// Message could contain commas, so we just use the rest of the string
					this.Message = input.Substring(parts[0].Length + parts[1].Length + parts[2].Length + 3);
				}
				else
				{
					// Prevent lines that aren't in the correct delimited form from being displayed.
					this.IsValid = false;
				}
			}

			public Boolean IsMatch(ViewModels.Admin.SystemIndex.LogSettingsViewModel logSettings)
			{
				if (!IsLevelSelected(logSettings))
				{
					return false;
				}
				else
				{
					return
						String.IsNullOrEmpty(logSettings.LogFilterTerm) ||
						this.Category.Contains(logSettings.LogFilterTerm, StringComparison.OrdinalIgnoreCase) ||
						this.Message.Contains(logSettings.LogFilterTerm, StringComparison.OrdinalIgnoreCase);
				}
			}

			private Boolean IsLevelSelected(ViewModels.Admin.SystemIndex.LogSettingsViewModel logSettings)
			{
				switch (this.Level)
				{
					case nameof(LogLevel.Trace):
						return logSettings.LogIncludeTrace;
					case nameof(LogLevel.Information):
						return logSettings.LogIncludeInformation;
					case nameof(LogLevel.Error):
						return logSettings.LogIncludeError;
					case nameof(LogLevel.Warning):
						return logSettings.LogIncludeWarning;
					default:
						return true;
				}
			}

			public override string ToString()
			{
				return this.Raw;
			}
		}

		public class DatabaseConnection
		{
			public string Schema { get; set; }

			public string DatabaseType { get; set; }

			public string ConnectionString { get; set; }

			public Dictionary<string, string> DatabaseInformation { get; set; } = new();
		}
	}
}
