using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Modules.Forums.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.Permissions;

namespace Nucleus.Modules.Forums.ViewModels
{
	public class Settings
	{
		public IEnumerable<Group> Groups { get; set; }
	
		public GroupSettings GroupSettings { get; set; }
	}
}
