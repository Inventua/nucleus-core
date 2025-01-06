using Microsoft.Identity.Client;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Modules.Maps.MapGeocoders;
using Nucleus.Modules.Maps.MapProviders;
using System;
using System.Collections.Generic;

namespace Nucleus.Modules.Maps.ViewModels;

public class Settings 
{
  public const string DUMMY_APIKEY = "!@#NOT_CHANGED^&*";

  public string MapProviderTypeName { get; set; }

  //public string MapProviderSettingsView { get; set; }

  //public string MapProviderGeocodingView { get; set; }
  public MapProvider MapProvider {  get; set; }

  public IEnumerable<AvailableMapProvider> AvailableMapProviders { get; set; }

  public IEnumerable<GeocodingLocation> Locations { get; set; } = [];
  public Boolean ShowLocationPicker { get; set; }

  public string ApiKey { get; set; }

  public File SelectedFile { get; set; }

  public Folder SelectedFolder { get; set; }

  public string MapFileName { get; set; }

  public Models.Settings MapSettings { get; set; }

  public string Address { get; set; }

  public class AvailableMapProvider
  {
    public string TypeName { get; set; }
    public string Name { get; set; }
  }

}
