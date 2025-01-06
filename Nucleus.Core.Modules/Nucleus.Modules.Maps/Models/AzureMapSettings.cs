using System;
using System.Collections.Generic;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.Maps.Models;

public class AzureMapSettings : Settings
{
  private class SettingsKeys
  {
    public const string SETTING_AZURE_MAPS_API_KEY = "maps:azure:apikey";
    public const string SETTING_AZURE_CLIENT_ID = "maps:azure:clientid";
    //public const string SETTING_AZURE_MAP_TYPE = "maps:azure:map-type";
  }

  //public readonly Dictionary<string, string> MapTypes = new () 
  //{
  //  { Azure.Maps.Rendering.MapTileSetId.MicrosoftBaseRoad.ToString(), "All Layers - Light" },
  //  { Azure.Maps.Rendering.MapTileSetId.MicrosoftBaseDarkgrey.ToString(), "All Layers - Dark" },
  //  { Azure.Maps.Rendering.MapTileSetId.MicrosoftBaseHybridRoad.ToString(), "Road, boundary and label data - Light" },
  //  { Azure.Maps.Rendering.MapTileSetId.MicrosoftBaseHybridDarkgrey.ToString(), "Road, boundary and label data - Dark" },
  //  { Azure.Maps.Rendering.MapTileSetId.MicrosoftImagery.ToString(), "Satellite Image" }
    
  //  // These are overlays which are meant to be drawn over the top of a base map, so they aren't useful for a static map.
  //  //{ Azure.Maps.Rendering.MapTileSetId.MicrosoftBaseLabelsRoad.ToString(), "Labels - Light" },
  //  //{ Azure.Maps.Rendering.MapTileSetId.MicrosoftBaseLabelsDarkgrey.ToString(), "Labels - Dark" },
  //  //{ Azure.Maps.Rendering.MapTileSetId.MicrosoftTerraMain.ToString(), "Shaded relief and terra layers" },
  //  //{ Azure.Maps.Rendering.MapTileSetId.MicrosoftWeatherInfraredMain.ToString(), "Weather Infrared" },
  //  //{ Azure.Maps.Rendering.MapTileSetId.MicrosoftWeatherRadarMain.ToString(), "Weather Radar" }
  //};

  public string AzureClientId { get; set; }

  public string EncryptedAzureApiKey { get; set; }  // Encrypted azure account id

  public override Boolean IsSubscriptionKeySet => !String.IsNullOrEmpty(this.EncryptedAzureApiKey);

  /// <summary>
  /// Returns Azure specific settings
  /// </summary>
  /// <param name="module"></param>
  public override void GetSettings(PageModule module)
  {
    base.GetSettings(module);

    this.AzureClientId = module.ModuleSettings.Get(SettingsKeys.SETTING_AZURE_CLIENT_ID, this.AzureClientId);
    this.EncryptedAzureApiKey = module.ModuleSettings.Get(SettingsKeys.SETTING_AZURE_MAPS_API_KEY, this.EncryptedAzureApiKey);

   // this.MapType = module.ModuleSettings.Get(SettingsKeys.SETTING_AZURE_MAP_TYPE, this.MapType);
  }

  public override void SetSettings(Site site, PageModule module, string apiKey)
  {
    base.SetSettings(site, module, apiKey);

    if (apiKey != ViewModels.Settings.DUMMY_APIKEY)
    {
      SetApiKey(site, apiKey);
    }
    else if (string.IsNullOrEmpty(apiKey))
    {
      SetApiKey(site, "");
    }

    module.ModuleSettings.Set(SettingsKeys.SETTING_AZURE_CLIENT_ID, this.AzureClientId);
    module.ModuleSettings.Set(SettingsKeys.SETTING_AZURE_MAPS_API_KEY, this.EncryptedAzureApiKey);
    //module.ModuleSettings.Set(SettingsKeys.SETTING_AZURE_MAP_TYPE, this.MapType);
  }

  public override string GetApiKey(Site site)
  {
    return (string.IsNullOrEmpty(this.EncryptedAzureApiKey) ? "" : Decrypt(site, this.EncryptedAzureApiKey));
  }

  public override void SetApiKey(Site site, string key)
  {
    this.EncryptedAzureApiKey = Encrypt(site, key);    
  }
}
