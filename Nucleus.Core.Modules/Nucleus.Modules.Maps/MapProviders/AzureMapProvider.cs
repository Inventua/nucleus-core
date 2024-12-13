using Nucleus.Modules.Maps.MapRenderers;
using Nucleus.Modules.Maps.MapGeocoders;
using Nucleus.Modules.Maps.Models;

namespace Nucleus.Modules.Maps.MapProviders;

internal class AzureMapProvider : IMapProvider
{
  public IMapRenderer GetRenderer() => new AzureMapRenderer();

  public IMapGeocoder GetGeocoder() => new AzureMapGeocoder();

  public Settings GetSettings() => new AzureMapSettings();
}
