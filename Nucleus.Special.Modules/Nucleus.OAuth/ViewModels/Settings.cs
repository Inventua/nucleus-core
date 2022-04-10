using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Extensions;

namespace Nucleus.OAuth.ViewModels
{
	public class Settings
	{
		public string Layout { get; set; }
		public List<string> Layouts { get; set; }

	}
}
