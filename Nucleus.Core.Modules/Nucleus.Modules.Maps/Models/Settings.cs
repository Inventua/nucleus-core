using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Maps.Models;

public class Settings
{
  private class ModuleSettingsKeys
  {
    public const string MODULESETTING_MAPPROVIDER = "maps:mapprovider";

    public const string MODULESETTING_MAPS_ZOOM = "maps:zoom";
    public const string MODULESETTING_MAPS_LONGITUDE = "maps:longitude";
    public const string MODULESETTING_MAPS_LATITUDE = "maps:latitude";
    public const string MODULESETTING_MAPS_HEIGHT = "maps:height";
    public const string MODULESETTING_MAPS_WIDTH = "maps:width";
    public const string MODULESETTING_MAPS_MAP_FILE_ID = "maps:map-file:id";
  }
  public const string DEFAULT_MAPPROVIDER = "AzureMaps";

  public static string GetMapProvider(PageModule module)
  {
    return module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_MAPPROVIDER, DEFAULT_MAPPROVIDER);
  }

  public static void SetMapProvider(PageModule module, string value)
  {
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_MAPPROVIDER, value);
  }

  // Azure zoom/tile grid info https://learn.microsoft.com/en-au/azure/azure-maps/zoom-levels-and-tile-grid
  // Google Maps https://developers.google.com/maps/documentation/maps-static/start#Zoomlevels
  public int Zoom { get; set; } = 10;
  public double Longitude { get; set; } 
  public double Latitude { get; set; }
  public int Height { get; set; } = 500;
  public int Width { get; set; } = 500;
  public Guid MapFileId { get; set; }
 

  public virtual void GetSettings(PageModule module)
  {
    this.Zoom = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_MAPS_ZOOM, this.Zoom);
    this.Latitude = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_MAPS_LATITUDE, this.Latitude);
    this.Longitude = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_MAPS_LONGITUDE, this.Longitude);
    this.Width = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_MAPS_WIDTH, this.Width);
    this.Height = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_MAPS_HEIGHT, this.Height);
    this.MapFileId = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_MAPS_MAP_FILE_ID, this.MapFileId);
  }


  public virtual void SetSettings(Site site, PageModule module, string apiKey)
  {
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_MAPS_ZOOM, this.Zoom);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_MAPS_LATITUDE, this.Latitude);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_MAPS_LONGITUDE, this.Longitude);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_MAPS_WIDTH, this.Width);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_MAPS_HEIGHT, this.Height);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_MAPS_MAP_FILE_ID, this.MapFileId);
  }

  public virtual void SetApiKey(PageModule module, string key)
  {
    throw new NotImplementedException();
  }


  public virtual string GetApiKey(Site site)
  {
    throw new NotImplementedException();
  }


  public virtual Boolean IsSubscriptionKeySet { get; }

  /// <summary>
  /// Encrypt and encode a secret key and return the result.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="apiKey"></param>
  /// <returns></returns>
  protected static string Encrypt(Site site, string apiKey)
  {
    if (String.IsNullOrEmpty(apiKey))
    {
      return null;
    }

    // Convert string to byte array
    byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes(apiKey);

    // Preparing the memory stream for encrypted string.
    System.IO.MemoryStream msOut = new();

    // Create the ICryptoTransform instance.
    System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create();
    aes.Key = site.Id.ToByteArray();
    aes.IV = site.Id.ToByteArray();

    // Create the CryptoStream instance.
    System.Security.Cryptography.CryptoStream cryptStream = new(msOut, aes.CreateEncryptor(aes.Key, aes.IV), System.Security.Cryptography.CryptoStreamMode.Write);

    // Encoding.
    cryptStream.Write(bytesIn, 0, bytesIn.Length);
    cryptStream.FlushFinalBlock();

    // Get the encrypted byte array.
    byte[] bytesOut = msOut.ToArray();

    cryptStream.Close();
    msOut.Close();

    // Convert to string and return result value
    return System.Convert.ToBase64String(bytesOut);
  }

  /// <summary>
  /// Decrypt and decode a secret key and return the result.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="secretKey"></param>
  /// <returns></returns>
  protected static string Decrypt(Site site, string apiKey)
  {
    if (String.IsNullOrEmpty(apiKey))
    {
      return null;
    }

    // Convert string to byte array
    byte[] bytesIn = System.Convert.FromBase64String(apiKey);

    // Preparing the memory stream for encrypted string.
    System.IO.MemoryStream msOut = new();

    // Create the ICryptoTransform instance.
    System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create();
    aes.Key = site.Id.ToByteArray();
    aes.IV = site.Id.ToByteArray();

    // Create the CryptoStream instance.
    System.Security.Cryptography.CryptoStream cryptStream = new(msOut, aes.CreateDecryptor(aes.Key, aes.IV), System.Security.Cryptography.CryptoStreamMode.Write);

    // Encoding.
    cryptStream.Write(bytesIn, 0, bytesIn.Length);
    cryptStream.FlushFinalBlock();

    // Get the encrypted byte array.
    byte[] bytesOut = msOut.ToArray();

    cryptStream.Close();
    msOut.Close();

    // Convert to string and return result value
    return System.Text.Encoding.UTF8.GetString(bytesOut);
  }
}
