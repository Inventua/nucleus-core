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

		public IEnumerable<string> AvailableServiceTypes { get; set; }

		public List<Nucleus.Web.ViewModels.Shared.LogFileInfo> LogFiles { get; set; }

		public string LogFile { get; set; }

		public string LogContent { get; set; }

		
	}
}
