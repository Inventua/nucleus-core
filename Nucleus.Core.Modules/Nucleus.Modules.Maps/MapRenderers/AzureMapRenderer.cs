using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Maps.Rendering;
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Maps.Models;

namespace Nucleus.Modules.Maps.MapRenderers;

public class AzureMapRenderer : IMapRenderer
{
  public async Task<Stream> RenderMap(Site site, IHttpClientFactory httpClientFactory, Settings settings)
  {
    //return await RenderAzureMap(settings.GetApiKey(site), settings.Zoom, settings.Longitude, settings.Latitude, settings.Height, settings.Width); 
    return await RenderAzureMap(GetRenderingClient(site, settings), settings.Zoom, settings.Longitude, settings.Latitude, settings.Height, settings.Width);
  }

  /// <summary>
  /// Get the rendering client by using API key or client id if API key is not present.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="settings"></param>
  /// <returns></returns>
  private MapsRenderingClient GetRenderingClient(Site site, Settings settings)
  {
    MapsRenderingClient client;
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

  //private static async Task<System.IO.Stream> RenderAzureMap(string apiKey, int zoom, double longitude, double latitude, int height, int width)
  //{    
  //  Azure.AzureKeyCredential credential = new(apiKey);
  //  MapsRenderingClient client = new(credential);

  //  MapTileIndex tileIndex = MapsRenderingClient.PositionToTileXY(new Azure.Core.GeoJson.GeoPosition(longitude, latitude), zoom, width);

  //  GetMapTileOptions mapTileOptions = new(MapTileSetId.MicrosoftImagery, tileIndex); //new MapTileIndex(tileIndex.X, tileIndex.Y, zoom));

  //  Azure.Response<System.IO.Stream> mapTile = await client.GetMapTileAsync(mapTileOptions);

  //  return mapTile;
  //}

  private static async Task<System.IO.Stream> RenderAzureMap(MapsRenderingClient client, int zoom, double longitude, double latitude, int height, int width)
  {
    MapTileIndex tileIndex = MapsRenderingClient.PositionToTileXY(new Azure.Core.GeoJson.GeoPosition(longitude, latitude), zoom, width);

    GetMapTileOptions mapTileOptions = new(MapTileSetId.MicrosoftImagery, tileIndex); //new MapTileIndex(tileIndex.X, tileIndex.Y, zoom));

    Azure.Response<System.IO.Stream> mapTile = await client.GetMapTileAsync(mapTileOptions);

    return mapTile;
  }

}
