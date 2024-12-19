using System;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.Maps.Models;

public class GoogleMapSettings : Settings
{
  public class SettingsKeys
  {
    public const string SETTING_GOOGLE_MAPS_API_KEY = "maps:google:apikey";
    public const string SETTING_GOOGLE_MAPS_TYPE = "maps:google:maptype";
    public const string SETTING_GOOGLE_MAPS_SCALE = "maps:google:scale";

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

    this.EncryptedGoogleApiKey = module.ModuleSettings.Get(SettingsKeys.SETTING_GOOGLE_MAPS_API_KEY, this.EncryptedGoogleApiKey);
    this.MapType = module.ModuleSettings.Get(SettingsKeys.SETTING_GOOGLE_MAPS_TYPE, this.MapType);
    this.Scale = module.ModuleSettings.Get(SettingsKeys.SETTING_GOOGLE_MAPS_SCALE, this.Scale);

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

    module.ModuleSettings.Set(SettingsKeys.SETTING_GOOGLE_MAPS_API_KEY, this.EncryptedGoogleApiKey);
    module.ModuleSettings.Set(SettingsKeys.SETTING_GOOGLE_MAPS_TYPE, this.MapType);
    module.ModuleSettings.Set(SettingsKeys.SETTING_GOOGLE_MAPS_SCALE, this.Scale);

  }

  public override string GetApiKey(Site site)
  {
    return (string.IsNullOrEmpty(this.EncryptedGoogleApiKey) ? "" : Decrypt(site, this.EncryptedGoogleApiKey));
  }

  public override void SetApiKey(Site site, string key)
  {
    this.EncryptedGoogleApiKey = Encrypt(site, key);
  }

}
