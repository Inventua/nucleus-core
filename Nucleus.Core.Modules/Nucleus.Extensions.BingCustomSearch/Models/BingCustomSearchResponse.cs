using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Nucleus.Extensions.BingCustomSearch.Models;

public class BingCustomSearchResponse
{
  [JsonPropertyName("webPages")]
  public WebPages WebPages { get; set; }
}

public class WebPages
{
  [JsonPropertyName("value")]
  public IList<WebPage> Value { get; set; }

  [JsonPropertyName("totalEstimatedMatches")]
  public long? TotalEstimatedMatches { get; set; }

}

public class WebPage
{
  [JsonPropertyName("name")]
  public string Name { get; set; }

  [JsonPropertyName("url")]
  public string Url { get; set; }

  [JsonPropertyName("snippet")]
  public string Snippet { get; set; }
  //public OpenGraphImage Thumbnail { get; set; }
}

//public class OpenGraphImage
//{
//  public int Width { get; set; }
//  public int Height { get; set; }
//}
