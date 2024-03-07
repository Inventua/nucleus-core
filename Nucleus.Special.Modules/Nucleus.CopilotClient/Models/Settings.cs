using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.CopilotClient.Models;
public class Settings
{
  private class ModuleSettingsKeys
  {
    public const string MODULESETTING_TOKEN_ENDPOINT = "copilotclient:direct-line-token-endpoint";
  }

  public string TokenEndpoint { get; set; }

  public void GetSettings(PageModule module)
  {
    this.TokenEndpoint = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_TOKEN_ENDPOINT, "");
  }

  public void SetSettings(PageModule module)
  {
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_TOKEN_ENDPOINT, this.TokenEndpoint);
  }
}

