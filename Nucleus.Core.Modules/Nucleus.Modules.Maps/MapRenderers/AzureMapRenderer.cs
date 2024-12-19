using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Maps.Rendering;
using Nucleus.Abstractions.Models;
using Nucleus.Modules.Maps.Models;

namespace Nucleus.Modules.Maps.MapRenderers;

public class AzureMapRenderer : IMapRenderer
{
  private Site Site { get; }

  public AzureMapRenderer(Context context)
  {
    this.Site = context.Site;
  }

  public async Task<Stream> RenderMap(Settings settings)
  {
    return await RenderAzureMap(GetRenderingClient(settings), settings.Zoom, settings.Longitude, settings.Latitude, settings.Height, settings.Width, settings.ShowMarker);
  }

  /// <summary>
  /// Get the rendering client by using API key or client id if API key is not present.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="settings"></param>
  /// <returns></returns>
  private MapsRenderingClient GetRenderingClient(Settings settings)
  {
    string apiKey = settings.GetApiKey(this.Site);

    return 
      !string.IsNullOrEmpty(apiKey) ? 
        new(new Azure.AzureKeyCredential(apiKey)) : 
        new(new DefaultAzureCredential(), (settings as AzureMapSettings)?.AzureClientId);
  }

  private static async Task<System.IO.Stream> RenderAzureMap(MapsRenderingClient client, int zoom, double longitude, double latitude, int height, int width, Boolean showMarker)
  {
    //private const int TILE_SIZE = 512; // roadmap
    //MapTileIndex tileIndex = MapsRenderingClient.PositionToTileXY(new Azure.Core.GeoJson.GeoPosition(longitude, latitude), zoom, TILE_SIZE);
    //GetMapTileOptions mapTileOptions = new(string.IsNullOrEmpty(mapStyle) ? MapTileSetId.MicrosoftBaseRoad : mapStyle, tileIndex) ;    
    //Azure.Response<System.IO.Stream> mapTile = await client.GetMapTileAsync(mapTileOptions);

    List<ImagePushpinStyle> markers = showMarker ? [new([new PushpinPosition(longitude, latitude)])] : [];
    GetMapStaticImageOptions options = new(new Azure.Core.GeoJson.GeoPosition(longitude, latitude), width, height, markers) 
    {
      ZoomLevel = zoom
    };
    
    return await client.GetMapStaticImageAsync(options);
  }
}
