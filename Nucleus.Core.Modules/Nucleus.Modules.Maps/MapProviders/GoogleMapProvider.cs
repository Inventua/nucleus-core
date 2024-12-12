using Nucleus.Modules.Maps.MapRenderers;
using Nucleus.Modules.Maps.MapGeocoders;
using Nucleus.Modules.Maps.Models;

namespace Nucleus.Modules.Maps.MapProviders;

internal class GoogleMapProvider : IMapProvider
{
  public IMapRenderer GetRenderer() => new GoogleMapRenderer();

  public IMapGeocoder GetGeocoder() => new GoogleMapGeocoder();

  public Settings GetSettings() => new GoogleMapSettings();
}
