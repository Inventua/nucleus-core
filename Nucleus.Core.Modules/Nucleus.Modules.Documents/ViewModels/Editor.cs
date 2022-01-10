using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Modules.Documents.Models;

namespace Nucleus.Modules.Documents.ViewModels
{
	public class Editor
	{				
		public Document SelectedDocument { get; set; }
		
		public IList<ListItem> Categories { get; set; }
	}
}
