using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.PageLinks.ViewModels
{
	public class Viewer : Models.Settings
	{
    public string EnabledHeaders { get; set; }
  }
}
