using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.PageLinks.ViewModels;

public class Settings : Models.Settings
{
	public Boolean HeaderH1 {  get; set; }
	public Boolean HeaderH2 { get; set; }
	public Boolean HeaderH3 { get; set; }
	public Boolean HeaderH4 { get; set; }
	public Boolean HeaderH5 { get; set; }
	public Boolean HeaderH6 { get; set; }
}
