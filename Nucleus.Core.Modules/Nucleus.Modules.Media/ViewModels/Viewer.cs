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
	public class Viewer : Models.Settings
	{
		public enum MediaTypes
		{
			None,
			Generic,
			Video,
			Image,
			PDF,
      YouTube
		}

		public Boolean PermissionDenied { get; set; }
    		
		public MediaTypes SelectedItemType { get; set; }
    
		public string Style { get; set; }

    public string SourceUrl { get; set; }
	}
}
