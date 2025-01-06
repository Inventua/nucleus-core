using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Maps.Models;

namespace Nucleus.Modules.Maps.MapRenderers;

public class GoogleMapRenderer : IMapRenderer
{
  private Site Site { get; }
  private IHttpClientFactory HttpClientFactory { get; }

  public GoogleMapRenderer(Context context, IHttpClientFactory httpClientFactory)
  {
    this.Site = context.Site;
    this.HttpClientFactory = httpClientFactory;
  }

  public async Task<Stream> RenderMap(Settings settings)
  {
    return await RenderGoogleMap(settings.GetApiKey(this.Site), settings.Zoom, settings.Longitude, settings.Latitude, settings.Height, settings.Width, (settings as GoogleMapSettings)?.MapType ?? GoogleMapSettings.MapTypes.Roadmap, (settings as GoogleMapSettings).Scale, (settings as GoogleMapSettings).ShowMarker);
  }

  private async Task<System.IO.Stream> RenderGoogleMap(string apiKey, int zoom, double longitude, double latitude, int height, int width, Models.GoogleMapSettings.MapTypes mapType, int scale, Boolean showMarker)
  {
    // https://developers.google.com/maps/documentation/maps-static/start
    Dictionary<string, object> parameters = new()
    {
      { "center", $"{latitude},{longitude}" },
      { "zoom", zoom },
      { "size", $"{width}x{height}" },
      { "maptype", mapType.ToString().ToLower() },
      { "scale", scale }
    };

    if (showMarker)
    {
      parameters.Add("markers", $"color:red|{latitude},{longitude}");
    }

    HttpClient httpClient = this.HttpClientFactory.CreateClient();

    HttpRequestMessage request = new(HttpMethod.Get, new System.Uri($"https://maps.googleapis.com/maps/api/staticmap?{BuildParameters(parameters)}&key={apiKey}"));

    HttpResponseMessage response = await httpClient.SendAsync(request);

    response.EnsureSuccessStatusCode();

    return await response.Content.ReadAsStreamAsync();
  }

  private static string BuildParameters(Dictionary<string, object> parameters)
  {
    return string.Join("&", parameters.Select(parameter => $"{parameter.Key}={parameter.Value}"));
  }
}
