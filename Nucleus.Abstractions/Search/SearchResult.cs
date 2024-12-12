using System;
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
  /// Date/Time that the index entry was created or updated, if available.
  /// </summary>
  public DateTime? IndexedDate { get; set; }

  /// <summary>
  /// A list of terms which were matched to the document.
  /// </summary>
  public List<string> MatchedTerms { get; set; }
}
