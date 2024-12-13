using Nucleus.Abstractions.Models.FileSystem;
using System.Collections.Generic;

namespace Nucleus.Modules.Maps.ViewModels;

public class Settings 
{
  public const string DUMMY_APIKEY = "!@#NOT_CHANGED^&*";

  public string MapProvider { get; set; }

  public string MapProviderSettingsView { get; set; }

  public string MapProviderGeocodingView { get; set; }

  public List<string> MapProviders { get; set; }

  public string ApiKey { get; set; }

  public File SelectedFile { get; set; }

  public Folder SelectedFolder { get; set; }

  public string MapFileName { get; set; }

  public Models.Settings MapSettings { get; set; }

  public string Address { get; set; }
}
