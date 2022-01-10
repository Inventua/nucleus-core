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
	public class Viewer
	{
		public enum MediaTypes
		{
			None,
			Generic,
			Video,
			Image,
			PDF
		}

		public Boolean PermissionDenied { get; set; }

		public File SelectedFile { get; set; }

		public MediaTypes SelectedItemType { get; set; }

		public string Caption { get; set; }
		public string AlternateText { get; set; }
		public Boolean ShowCaption { get; set; }

		public string Height { get; set; }
		public string Width { get; set; }

		public Boolean AlwaysDownload { get; set; }

		public string Style { get; set; }
	}
}
