using System.Collections.Generic;

namespace Nucleus.Abstractions.Search;

/// <summary>
/// Represents a semantic search result.
/// </summary>
/// <remarks>
/// Refer to <seealso cref="ContentMetaData"/> for more information.
/// </remarks>
public class SemanticResult : SearchResult
{
  /// <summary>
  /// The "answer" text/html returned for the result.
  /// </summary>
  public string Answer { get; set; }
}
