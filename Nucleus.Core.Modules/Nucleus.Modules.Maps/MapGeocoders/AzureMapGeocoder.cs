using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Maps.Search;
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Maps.Models;

namespace Nucleus.Modules.Maps.MapGeocoders;

public class AzureMapGeocoder : IMapGeocoder
{
  public async Task<Models.GeocodingLocation> LookupAddress(Site site, IHttpClientFactory httpClientFactory, Settings settings, string address)
  {
    return await GetGeocodeLocation(GetSearchClient(site, settings), address);
  }


  /// <summary>
  /// Get the search client by using API key or client id if API key is not present.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="settings"></param>
  /// <returns></returns>
  private MapsSearchClient GetSearchClient(Site site, Settings settings)
  {
    MapsSearchClient client;
    
    string apiKey = settings.GetApiKey(site);
    if (!string.IsNullOrEmpty(apiKey))
    {
      Azure.AzureKeyCredential credential = new(apiKey);
      client = new(credential);
    }
    else
    {
      TokenCredential clientCredential = new DefaultAzureCredential();
      client = new(clientCredential, (settings as AzureMapSettings)?.AzureClientId);
    }
    return client;
  }

  private static async Task<Models.GeocodingLocation> GetGeocodeLocation(MapsSearchClient client, string address)
  {
    Azure.Response<Azure.Maps.Search.Models.GeocodingResponse> response = await client.GetGeocodingAsync(address);

    return BuildGeocodingLocation(response.Value.Features);
  }

  private static Models.GeocodingLocation BuildGeocodingLocation(IReadOnlyList<Azure.Maps.Search.Models.FeaturesItem> features)
  {
    Models.GeocodingLocation geocodingLocation = new();

    if (features != null && features.Count > 0)
    {
      //
      geocodingLocation.Address = features[0].Properties.Address.FormattedAddress;
      geocodingLocation.LocationTypes = new List<string>()
      {
        features[0].Geometry.Type.ToString()
      };
      geocodingLocation.Geometry.Latitude = features[0].Geometry.Coordinates.Latitude;
      geocodingLocation.Geometry.Longitude = features[0].Geometry.Coordinates.Longitude;
    }

    return geocodingLocation;
  }
}
