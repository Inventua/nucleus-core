using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Nucleus.Extensions.BingCustomSearch.Models;

public class BingCustomSuggestions
{
  [JsonPropertyName("suggestionGroups")]
  public SuggestionGroups SuggestionGroups { get; set; }
}

public class SuggestionGroups
{
  [JsonPropertyName("name")]
  public string Name { get; set; }

  [JsonPropertyName("searchSuggestions")]
  public IList<SearchAction> SearchSuggestions { get; set; }
}

public class SearchAction
{
  [JsonPropertyName("displayText")]
  public string DisplayText { get; set; }

  [JsonPropertyName("query")]
  public string Query { get; set; }

  [JsonPropertyName("searchKind")]
  public string SearchKind { get; set; }
}
