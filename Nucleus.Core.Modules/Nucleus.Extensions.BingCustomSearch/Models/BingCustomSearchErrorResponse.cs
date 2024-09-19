using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nucleus.Extensions.BingCustomSearch.Models;

public class BingCustomSearchErrorResponse
{
  [JsonPropertyName("errors")]
  public List<BingCustomSearchError> Errors { get; set; }

  [JsonPropertyName("error")]
  public BingCustomSearchError Error
  {
    set 
    {
      this.Errors = [ value ];
    }
  }
}
