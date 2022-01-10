using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.StaticContent.ViewModels
{
	public class Settings
	{
		public Folder SourceFolder { get; set; }
		public File DefaultFile { get; set; }
	}
}
