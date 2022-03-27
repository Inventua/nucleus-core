using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

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

		public List<Shared.LogFileInfo> LogFiles { get; set; }

		public string LogFile { get; set; }

		public List<LogEntry> LogContent { get; set; }
		public string LogMessage { get; set; }

		public string Configuration { get; set; }

		public IEnumerable<Nucleus.Abstractions.Models.TaskScheduler.RunningTask> RunningTasks { get; set; }

		public IEnumerable<DatabaseConnection> DatabaseConnections { get; set; }
		public long UsersOnline { get; set; }

		//public Dictionary<string, string> WebServerInformation { get; set; } = new();

		public SystemIndex()
		{
		}

		public class LogEntry
		{
			public DateTime Date { get; set; }
			public string Level { get; set; }
			public string Category { get; set; }
			public string Message { get; set; }
			public Boolean IsValid { get; set; } = true;

			public LogEntry(string input)
			{
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
