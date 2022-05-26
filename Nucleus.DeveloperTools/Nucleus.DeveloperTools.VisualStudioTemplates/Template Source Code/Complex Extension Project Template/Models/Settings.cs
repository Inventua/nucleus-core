using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace $nucleus_extension_namespace$.Models
{
	public class Settings
	{
		public const string MODULESETTING_TITLE = "$nucleus_extension_name$:title";

		public string Title { get; set; }

    public void ReadSettings(PageModule module)
    {
      this.Title = module.ModuleSettings.Get(MODULESETTING_TITLE, "");      
    }
  }
}
