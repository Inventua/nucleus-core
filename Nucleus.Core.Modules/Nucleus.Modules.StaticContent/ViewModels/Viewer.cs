using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.StaticContent.ViewModels
{
	public class Viewer : Settings
	{
		public string Content { get; set; }
	}
}
