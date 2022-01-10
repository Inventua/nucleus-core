using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nucleus.Modules.Media.ViewModels
{
	public class Settings
	{
		
		//public string SelectedProviderKey { get; set; }

		//public Dictionary<string, Nucleus.Core.FileSystemProviders.FileSystemProviderInfo> Providers { get; set; }
		//public Folder Current { get; set; } = new();
		public File SelectedFile { get; set; }

		public string Height { get; set; }
		public string Width { get; set; }

		public Boolean AlwaysDownload { get; set; }

		public string Caption { get; set; }
		public string AlternateText { get; set; }
		public Boolean ShowCaption { get; set; }
	}
}
