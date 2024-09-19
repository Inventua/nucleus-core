using System.Text.Json.Serialization;

namespace Nucleus.Extensions.BingCustomSearch.Models;

public class BingCustomSearchError
{
  [JsonPropertyName("code")]
  public string Code { get; set; }
  [JsonPropertyName("subCode")]
  public string SubCode { get; set; }
  [JsonPropertyName("message")]
  public string Message{ get; set; }
}
