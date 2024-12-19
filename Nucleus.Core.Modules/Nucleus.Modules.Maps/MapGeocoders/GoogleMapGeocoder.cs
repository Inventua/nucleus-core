using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Maps.Models;

namespace Nucleus.Modules.Maps.MapGeocoders;

public class GoogleMapGeocoder : IMapGeocoder
{
  private Site Site { get; }
  private IHttpClientFactory HttpClientFactory { get; }

  public GoogleMapGeocoder(Context context, IHttpClientFactory httpClientFactory)
  {
    this.Site = context.Site;
    this.HttpClientFactory = httpClientFactory;
  }

  public async Task<IEnumerable<GeocodingLocation>> LookupAddress(Settings settings, string address)
  {
    return await GetGeocodeLocation(settings.GetApiKey(this.Site), address);
  }

  private async Task<IEnumerable<GeocodingLocation>> GetGeocodeLocation(string apiKey, string address)
  {
    HttpClient httpClient = this.HttpClientFactory.CreateClient();

    HttpRequestMessage request = new(HttpMethod.Get, new System.Uri($"https://maps.googleapis.com/maps/api/geocode/json?address={System.Web.HttpUtility.UrlEncode(address)}&key={apiKey}"));

    HttpResponseMessage response = await httpClient.SendAsync(request);

    response.EnsureSuccessStatusCode();

    // Deserialize the response
    GoogleGeocodingResponse geocodingResponse = await JsonSerializer.DeserializeAsync<GoogleGeocodingResponse>(await response.Content.ReadAsStreamAsync());

    return BuildGeocodingLocation(geocodingResponse);
  }

  private static IEnumerable<GeocodingLocation> BuildGeocodingLocation(GoogleGeocodingResponse geocodingResponse)
  {
    GeocodingLocation geocodingLocation = new();
    if (geocodingResponse.Results.Count > 0)
    {
      return geocodingResponse.Results.Select(result => new GeocodingLocation()
      {
        Address = result.FormattedAddress,
        LocationTypes = result.Types,

        Geometry = new()
        {
          Latitude = result.Geometry.Location.Latitude,
          Longitude = result.Geometry.Location.Longitude
        },

        Viewport = new() 
        {
          TopLeft=new()
          {
            Latitude = result.Geometry.Viewport.NorthEast.Latitude,
            Longitude = result.Geometry.Viewport.NorthEast.Longitude,
          },
          BottomRight=new()
          {
            Latitude = result.Geometry.Viewport.SouthWest.Latitude,
            Longitude = result.Geometry.Viewport.SouthWest.Longitude
          }
        }
      });

      //GoogleGeocodingResponse.GeocodingResult result = geocodingResponse.Results[0];
      //geocodingLocation = new()
      //{
      //  Address = result.FormattedAddress,
      //  LocationTypes = result.Types
      //};

      //geocodingLocation.Geometry.Latitude = result.Geometry.Location.Latitude;
      //geocodingLocation.Geometry.Longitude = result.Geometry.Location.Longitude;

      //geocodingLocation.Viewport.TopLeft.Latitude = result.Geometry.Viewport.NorthEast.Latitude;
      //geocodingLocation.Viewport.TopLeft.Longitude = result.Geometry.Viewport.NorthEast.Longitude;
      //geocodingLocation.Viewport.BottomRight.Latitude = result.Geometry.Viewport.SouthWest.Latitude;
      //geocodingLocation.Viewport.BottomRight.Longitude = result.Geometry.Viewport.SouthWest.Longitude;

      //return geocodingLocation;
    }

    return null;
  }
}
