using System.Collections.Generic;

namespace Nucleus.Modules.Maps.ViewModels;

public class Settings 
{
  public const string DUMMY_APIKEY = "!@#NOT_CHANGED^&*";

  public string MapProvider { get; set; }

  public string MapProviderSettingsView {  get; set; }

  public List<string> MapProviders { get; set; }

  public string ApiKey { get; set; }

  public Models.Settings MapSettings { get; set; }

  //public ViewDataDictionary ProviderSettings { get; set; }
}
