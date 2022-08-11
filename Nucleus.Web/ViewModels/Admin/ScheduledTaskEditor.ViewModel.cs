using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;

namespace Nucleus.Web.ViewModels.Admin
{
	public class ScheduledTaskEditor
	{
		public ScheduledTask ScheduledTask { get; set; }

		public ScheduledTaskHistory LatestHistory { get; set; }

		public List<ScheduledTaskHistory> History { get; set; }

		public IEnumerable<ServiceType> AvailableServiceTypes { get; set; }

		public LogSettingsViewModel LogSettings { get; set; } = new();
		//public List<Shared.LogFileInfo> LogFiles { get; set; }

		//public string LogFile { get; set; }

		//public Nucleus.Abstractions.Models.Paging.PagedResult<ViewModels.Admin.SystemIndex.LogEntry> LogContent { get; set; } = new() { PageSize = 100, PageSizes = new List<int>() { 100, 250, 500 } };

		//public string LogMessage { get; set; }

		public class ServiceType
		{
			public string FriendlyName { get; set; }
			public string TypeName { get; set; }

			public ServiceType(string friendlyName, string typeName)
			{
				this.FriendlyName = friendlyName;
				this.TypeName = typeName;
			}
		}

		public class LogSettingsViewModel : ViewModels.Admin.SystemIndex.LogSettingsViewModel {};
	}

	
}
