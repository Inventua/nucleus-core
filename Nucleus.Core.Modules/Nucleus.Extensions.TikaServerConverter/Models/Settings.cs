using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Extensions.TikaServerConverter.Models;
public class Settings
{
  private class ModuleSettingsKeys
  {
    internal const string SETTING_TIKA_SERVER_ENDPOINT = "tika-server-converter:tika-server-endpoint";
  }

  public string TikaServerEndpoint { get; set; }

  public void GetSettings(Site site)
  {
    if (site.SiteSettings.TryGetValue(ModuleSettingsKeys.SETTING_TIKA_SERVER_ENDPOINT, out string endpoint))
    {
      this.TikaServerEndpoint = endpoint;
    }
  }

  public void SetSettings(Site site)
  {
    site.SiteSettings.TrySetValue(ModuleSettingsKeys.SETTING_TIKA_SERVER_ENDPOINT, this.TikaServerEndpoint);
  }
}

