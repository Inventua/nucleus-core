using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Maps.Search;
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Maps.Models;

namespace Nucleus.Modules.Maps.MapGeocoders;

public class AzureMapGeocoder : IMapGeocoder
{
  private Site Site { get; }

  public AzureMapGeocoder(Context context)
  {
    this.Site = context.Site;
  }

  public async Task<IEnumerable<GeocodingLocation>> LookupAddress(Settings settings, string address)
  {
    return await GetGeocodeLocation(GetSearchClient(settings), address);
  }

  /// <summary>
  /// Get the search client by using API key or client id if API key is not present.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="settings"></param>
  /// <returns></returns>
  private MapsSearchClient GetSearchClient(Settings settings)
  {
    string apiKey = settings.GetApiKey(this.Site);

    return
      !string.IsNullOrEmpty(apiKey) ?
        new(new Azure.AzureKeyCredential(apiKey)) :
        new(new DefaultAzureCredential(), (settings as AzureMapSettings)?.AzureClientId);
  }

  private static async Task<IEnumerable<GeocodingLocation>> GetGeocodeLocation(MapsSearchClient client, string address)
  {
    Azure.Response<Azure.Maps.Search.Models.GeocodingResponse> response = await client.GetGeocodingAsync(address);

    return BuildGeocodingLocation(response.Value.Features);
  }

  private static IEnumerable<GeocodingLocation> BuildGeocodingLocation(IReadOnlyList<Azure.Maps.Search.Models.FeaturesItem> features)
  {
    GeocodingLocation geocodingLocation = new();

    if (features != null && features.Count > 0)
    {
      return features.Select(feature => new GeocodingLocation()
      {
        Address = feature.Properties.Address.FormattedAddress,
        LocationTypes = new List<string>()
        {
          feature.Geometry.Type.ToString()
        },
        Geometry = new() 
        {
          Latitude = feature.Geometry.Coordinates.Latitude,
          Longitude = feature.Geometry.Coordinates.Longitude
        }
      });
    }

    return [];
  }
}
