using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions.Shadow.Models;
public class Settings
{
  private class ModuleSettingsKeys
  {
    public const string MODULESETTING_PAGE_ID = "shadow:page-id";
    public const string MODULESETTING_MODULE_ID = "shadow:module-id";
  }

  public Guid PageId { get; set; }
  public Guid ModuleId { get; set; }

  public void GetSettings(PageModule module)
  {
    this.PageId = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_PAGE_ID, Guid.Empty);
    this.ModuleId = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_MODULE_ID, Guid.Empty);
  }

  public void SetSettings(PageModule module)
  {
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_PAGE_ID, this.PageId);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_MODULE_ID, this.ModuleId);
  }
}
