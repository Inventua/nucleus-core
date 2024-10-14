using System;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.Maps.Models;

public class GoogleMapSettings : Settings
{
  public class ModuleSettingsKeys
  {
    public const string MODULESETTING_GOOGLE_MAPS_API_KEY = "maps:google:apikey";
    public const string MODULESETTING_GOOGLE_MAPS_TYPE = "maps:google:maptype";
    public const string MODULESETTING_GOOGLE_MAPS_SCALE = "maps:google:scale";
  }

  /// <summary>
  /// Type of map to render
  /// </summary>
  public enum MapTypes
  {
    /// <summary>
    /// Default standard roadmap image
    /// </summary>
    Roadmap,
    /// <summary>
    /// Satellite image
    /// </summary>
    Satellite,
    /// <summary>
    /// Physical relief image depicting terrain and vegetation
    /// </summary>
    Terrain,
    /// <summary>
    /// Hybrid combination of roadmap and satellite image with major streets and place names on satellite image
    /// </summary>
    Hybrid
  }

  public string EncryptedGoogleApiKey { get; set; }

  public override Boolean IsSubscriptionKeySet => !String.IsNullOrEmpty(this.EncryptedGoogleApiKey);

  public MapTypes MapType { get; set; } = GoogleMapSettings.MapTypes.Roadmap;

  public int Scale { get; set; } = 1;

  public override void GetSettings(PageModule module)
  {
    base.GetSettings(module);

    this.EncryptedGoogleApiKey = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_GOOGLE_MAPS_API_KEY, this.EncryptedGoogleApiKey);
    this.MapType = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_GOOGLE_MAPS_TYPE, this.MapType);
    this.Scale = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_GOOGLE_MAPS_SCALE, this.Scale);
  }

  public override void SetSettings(Site site, PageModule module, string apiKey)
  {
    base.SetSettings(site, module, apiKey);

    if (apiKey != ViewModels.Settings.DUMMY_APIKEY)
    {
      SetApiKey(module, Encrypt(site, apiKey));
    }
    else if (string.IsNullOrEmpty(apiKey))
    {
      SetApiKey(module, "");
    }
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_GOOGLE_MAPS_TYPE, this.MapType);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_GOOGLE_MAPS_SCALE, this.Scale);
  }

  public override string GetApiKey(Site site)
  {
    return (string.IsNullOrEmpty(this.EncryptedGoogleApiKey) ? "" : Decrypt(site, this.EncryptedGoogleApiKey));
  }

  public override void SetApiKey(PageModule module, string key)
  {
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_GOOGLE_MAPS_API_KEY, key);
  }

}
