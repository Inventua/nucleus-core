using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Modules.Publish.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Publish.ViewModels
{
	public class Editor
	{
		public Models.Article Article { get; set; }
		
		public IEnumerable<List> Lists { get; set; }
		public List<CategorySelection> Categories { get; set; } = new();	
		public File SelectedAttachmentFile { get; set; }

	}
}
