using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nucleus.Extensions.BingCustomSearch.Models;

public class BingCustomSuggestions
{
  [JsonPropertyName("suggestionGroups")]
  public List<SuggestionGroup> SuggestionGroups { get; set; }
}

public class SuggestionGroup
{
  [JsonPropertyName("name")]
  public string Name { get; set; }

  [JsonPropertyName("searchSuggestions")]
  public List<SearchAction> SearchSuggestions { get; set; }
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
