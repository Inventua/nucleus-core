using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.ViewFeatures.ViewModels
{
	public class FileUpload
	{
		public string ControlName { get; set; }
		public string ActionName { get; set; }
		public string ControllerName { get; set; }
		public string AreaName { get; set; }
		public string ExtensionName { get; set; }
		public string Filter { get; set; }

		//public Dictionary<string, Nucleus.Core.FileSystemProviders.FileSystemProviderInfo> Providers { get; set; }
		//public Folder SelectedFolder { get; set; } = new();

		//public File SelectedFile { get; set; } = new();


	}
}
