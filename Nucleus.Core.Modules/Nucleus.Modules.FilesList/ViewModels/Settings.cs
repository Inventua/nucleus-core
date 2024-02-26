using Nucleus.Abstractions.Models.FileSystem;
using System.Collections.Generic;

namespace Nucleus.Modules.FilesList.ViewModels;

public class Settings : Models.Settings
{
	public List<string> Layouts { get; set; }
	public Folder SelectedFolder { get; set; }
}
