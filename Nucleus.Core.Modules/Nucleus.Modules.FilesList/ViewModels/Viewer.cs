using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.FilesList.ViewModels;

public class Viewer : Models.Settings
{
	public List<File> Files { get; set; } = new();
	public string LayoutPath { get; set; }

}
