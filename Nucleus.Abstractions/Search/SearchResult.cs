using System.Collections.Generic;

namespace Nucleus.Abstractions.Search;

/// <summary>
/// Represents a search result.
/// </summary>
/// <remarks>
/// Refer to <seealso cref="ContentMetaData"/> for more information.
/// </remarks>
public class SearchResult : ContentMetaData
{
  /// <summary>
  /// Search result score.
  /// </summary>
  public double? Score { get; set; }

  /// <summary>
  /// A list of terms which were matched to the document.
  /// </summary>
  public List<string> MatchedTerms { get; set; }
}
