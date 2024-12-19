using System.Collections.Generic;
using System.Threading.Tasks;
using Nucleus.Modules.Maps.Models;

namespace Nucleus.Modules.Maps.MapGeocoders;

public interface IMapGeocoder
{
  public Task<IEnumerable<GeocodingLocation>> LookupAddress(Settings settings, string address);
}
