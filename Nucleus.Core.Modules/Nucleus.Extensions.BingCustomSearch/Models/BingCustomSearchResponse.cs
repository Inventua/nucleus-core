using System.Collections.Generic;
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
}
