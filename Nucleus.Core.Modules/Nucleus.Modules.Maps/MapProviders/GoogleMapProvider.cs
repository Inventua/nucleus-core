using Nucleus.Modules.Maps.MapRenderers;
using Nucleus.Modules.Maps.MapGeocoders;
using Nucleus.Modules.Maps.Models;
using System.ComponentModel;
using Nucleus.Abstractions.Models;

namespace Nucleus.Modules.Maps.MapProviders;

[DisplayName("Google Maps")]
internal class GoogleMapProvider : MapProvider
{
  public GoogleMapProvider(GoogleMapRenderer renderer, GoogleMapGeocoder geocoder) : base(renderer, geocoder)
  {

  }

  public override GoogleMapSettings GetSettings(PageModule module)
  {
    GoogleMapSettings settings = new();
    settings.GetSettings(module);

    return settings;
  }

  public override string SettingsView() => $"MapSettings/_GoogleMapsSettings.cshtml";

}
