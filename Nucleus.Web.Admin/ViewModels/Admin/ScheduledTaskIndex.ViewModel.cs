using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.TaskScheduler;

namespace Nucleus.Web.ViewModels.Admin
{	
	public class ScheduledTaskIndex
	{
		public Nucleus.Abstractions.Models.Paging.PagedResult<ScheduledTask> ScheduledTasks { get; set; } = new() { PageSize = 20 };	

	}
}
