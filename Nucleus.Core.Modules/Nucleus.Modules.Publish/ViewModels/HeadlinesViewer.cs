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
using Nucleus.Abstractions.Models.Paging;

namespace Nucleus.Modules.Publish.ViewModels
{
	public class HeadlinesViewer :  Nucleus.Modules.Publish.ViewModels.Viewer
	{
		public HeadlinesSettings HeadlineSettings { get; set; } = new();
  }
}
