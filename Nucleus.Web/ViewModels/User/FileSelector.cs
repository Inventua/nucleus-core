using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Web.ViewModels.User
{
	public class FileSelector
	{
		public string Pattern { get; set; }
		public File File { get; set; }
		public Boolean ShowSelectAnother { get; set; } = true;
	}
}
