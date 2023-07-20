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
	public class Settings : Models.Settings
	{    
   
    public Dictionary<string, string> SourceTypes { get; set; }

     
	}
}
