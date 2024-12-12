using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Maps.Models;

namespace Nucleus.Modules.Maps.MapGeocoders;

public class GoogleMapGeocoder : IMapGeocoder
{
  public async Task<Models.GeocodingLocation> LookupAddress(Site site, IHttpClientFactory httpClientFactory, Settings settings, string address)
  {
    return await GetGeocodeLocation(httpClientFactory, settings.GetApiKey(site), address);
  }

  private static async Task<Models.GeocodingLocation> GetGeocodeLocation(IHttpClientFactory httpClientFactory, string apiKey, string address)
  {
    HttpClient httpClient = httpClientFactory.CreateClient();

    HttpRequestMessage request = new(HttpMethod.Get, new System.Uri($"https://maps.googleapis.com/maps/api/geocode/json?address={System.Web.HttpUtility.UrlEncode(address)}&key={apiKey}"));

    HttpResponseMessage response = await httpClient.SendAsync(request);

    response.EnsureSuccessStatusCode();

    // Deserialize the response
    GoogleGeocodingResponse geocodingResponse = await JsonSerializer.DeserializeAsync<GoogleGeocodingResponse>(await response.Content.ReadAsStreamAsync());

    //return geocodingResponse.ToString();
    return BuildGeocodingLocation(geocodingResponse);
  }

  private static Models.GeocodingLocation BuildGeocodingLocation(GoogleGeocodingResponse geocodingResponse)
  {
    Models.GeocodingLocation geocodingLocation = new();
    if (geocodingResponse.Results.Count > 0)
    {
      Models.GoogleGeocodingResponse.GeocodingResult result = geocodingResponse.Results[0];
      geocodingLocation = new()
      {
        Address = result.FormattedAddress,
        LocationTypes = result.Types
      };

      geocodingLocation.Geometry.Latitude = result.Geometry.Location.Latitude;
      geocodingLocation.Geometry.Longitude = result.Geometry.Location.Longitude;

      geocodingLocation.Viewport.TopLeft.Latitude = result.Geometry.Viewport.NorthEast.Latitude;
      geocodingLocation.Viewport.TopLeft.Longitude = result.Geometry.Viewport.NorthEast.Longitude;
      geocodingLocation.Viewport.BottomRight.Latitude = result.Geometry.Viewport.SouthWest.Latitude;
      geocodingLocation.Viewport.BottomRight.Longitude = result.Geometry.Viewport.SouthWest.Longitude;
    }

    return geocodingLocation;
  }
}
