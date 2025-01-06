using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Maps.MapGeocoders;
using Nucleus.Modules.Maps.MapRenderers;
using Nucleus.Modules.Maps.Models;

namespace Nucleus.Modules.Maps.MapProviders;

public abstract class MapProvider
{
  public IMapRenderer Renderer { get; protected set; }
  public IMapGeocoder Geocoder { get; protected set; }

  public abstract Settings GetSettings(PageModule module);

  public abstract string SettingsView();

  public MapProvider(IMapRenderer renderer,  IMapGeocoder geocoder)
  {
    this.Renderer = renderer;
    this.Geocoder = geocoder;
  }
}
