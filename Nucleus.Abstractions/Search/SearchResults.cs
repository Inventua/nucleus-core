using System.Collections.Generic;

namespace Nucleus.Abstractions.Search;

/// <summary>
/// Represents the search results for a search query.
/// </summary>
public class SearchResults
{
  /// <summary>
  /// A list of <see cref="SearchResult"/>s.
  /// </summary>
  /// <remarks>
  /// The results list contains results for the requested page.
  /// </remarks>
  public IEnumerable<SearchResult> Results { get; set; }

  /// <summary>
  /// Total number of results matching the search query.
  /// </summary>
  /// <remarks>
  /// When a page of results is requested, the result contains results for the requested page only.  The total property specifies the total
  /// number of available results and is used by paging controls.
  /// </remarks>
  public long Total { get; set; }

  /// <summary>
  /// Highest score for any search result.
  /// </summary>
  /// <remarks>
  /// This value can be compared with the <seealso cref="SearchResult.Score"/> of each search result to determine how well the result matches the search query."/>
  /// </remarks>
  public double? MaxScore { get; set; }

  /// <summary>
  /// A list of <see cref="SemanticResult"/>s representing semantic answers.
  /// </summary>
  /// <remarks>
  /// The answers list contains semantic results for the requested page. Not all search providers support semantic search, so this value will 
  /// often be null or empty.
  /// </remarks>
  public IEnumerable<SemanticResult> Answers { get; set; }
}
