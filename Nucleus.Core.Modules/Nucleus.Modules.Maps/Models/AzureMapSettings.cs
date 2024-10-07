using System;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.Maps.Models;

public class AzureMapSettings : Settings
{
  private class ModuleSettingsKeys
  {
    public const string MODULESETTING_AZURE_MAPS_API_KEY = "maps:azure:apikey";
    public const string MODULESETTING_AZURE_CLIENT_ID = "maps:azure:clientid";
  }

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

    this.AzureClientId = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_AZURE_CLIENT_ID, this.AzureClientId);
    this.EncryptedAzureApiKey = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_AZURE_MAPS_API_KEY, this.EncryptedAzureApiKey);
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
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_AZURE_CLIENT_ID, this.AzureClientId);
  }

  public override string GetApiKey(Site site)
  {
    return (string.IsNullOrEmpty(this.EncryptedAzureApiKey) ? "" : Decrypt(site, this.EncryptedAzureApiKey));
  }

  public override void SetApiKey(PageModule module, string key)
  {
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_AZURE_MAPS_API_KEY, key);
  }

}
