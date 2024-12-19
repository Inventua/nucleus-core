using Nucleus.Modules.Maps.MapRenderers;
using Nucleus.Modules.Maps.MapGeocoders;
using Nucleus.Modules.Maps.Models;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.Maps.MapProviders;

[DisplayName("Azure Maps")]
internal class AzureMapProvider : MapProvider
{
  public AzureMapProvider(AzureMapRenderer renderer, AzureMapGeocoder geocoder) : base(renderer, geocoder)
  {

  }

  public override AzureMapSettings GetSettings(PageModule module)
  {
    AzureMapSettings settings = new();
    settings.GetSettings(module);

    return settings;
  }

  public override string SettingsView() => $"MapSettings/_AzureMapsSettings.cshtml";
}
