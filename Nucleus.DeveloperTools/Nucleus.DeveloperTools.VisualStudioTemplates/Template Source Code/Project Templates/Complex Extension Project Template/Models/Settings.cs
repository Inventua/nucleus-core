using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace $nucleus.extension.namespace$.Models;

public class Settings
{
  private class ModuleSettingsKeys
  {
    public const string MODULESETTING_TITLE = "$nucleus.extension.name.lowercase$:title";
  }

  public string Title { get; set; }

  public void GetSettings(PageModule module)
  {
    this.Title = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_TITLE, "");
  }

  public void SetSettings(PageModule module)
  {
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_TITLE, this.Title);
  }
}
