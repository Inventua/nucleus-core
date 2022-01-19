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

		public List<Nucleus.Web.ViewModels.Shared.LogFileInfo> LogFiles { get; set; }

		public string LogFile { get; set; }

		public string LogContent { get; set; }

		public string Configuration { get; set; }

		public IEnumerable<Nucleus.Abstractions.Models.TaskScheduler.RunningTask> RunningTasks { get; set; }

		public IEnumerable<DatabaseConnection> DatabaseConnections { get; set; }

		public SystemIndex()
		{
		}

		public class DatabaseConnection
		{
			public string Schema { get; set; }

			public string DatabaseType { get; set; }

			public string ConnectionString { get; set; }

		}
	}
}
