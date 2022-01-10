using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.MultiContent.ViewModels
{
	public class Viewer
	{
		public List<Content> Contents { get; set; }
		public string Layout { get; set; }
		public PageModule Module { get; set; }

		public LayoutSettings LayoutSettings { get; set; }
	}
}
