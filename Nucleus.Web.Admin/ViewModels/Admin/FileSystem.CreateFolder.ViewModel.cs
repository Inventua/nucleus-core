using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Permissions;

namespace Nucleus.Web.ViewModels.Admin
{
	public class FileSystemCreateFolder
	{
		public string NewFolder { get; set; }

		public Folder Folder { get; set; } = new();		
	}
}
