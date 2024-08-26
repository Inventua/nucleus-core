using System;

namespace Nucleus.Abstractions.Search;

/// <summary>
/// Interface used by a search provider to report its cababilities and limitations.
/// </summary>
/// <remarks>
/// Capabilities data is used to control the availability of options in the Search module's settings page.
/// </remarks>
public interface ISearchProviderCapabilities
{
  /// <summary>
  /// Specifies the maximum page size supported by the search provider.
  /// </summary>
  public int MaximumPageSize { get; }

  /// <summary>
  /// Specifies the maximum number of search suggestions that the search provider can return. Return 0 if search suggestions are not supported.
  /// </summary>
  public int MaximumSuggestions { get; }

  /// <summary>
  /// Specifies whether the search provider can filter results by <see cref="ContentMetaData.Scope"/>.
  /// </summary>
  public Boolean CanFilterByScope { get; }


  /// <summary>
  /// Specifies that the search provider can return the <see cref="ContentMetaData.Size"/> of a result.
  /// </summary>
  public Boolean CanReportSize { get; }

  /// <summary>
  /// Specifies whether the search provider can return result <see cref="SearchResult.Score"/>.
  /// </summary>
  public Boolean CanReportScore { get; }

  /// <summary>
  /// Specifies whether the search provider can return the <see cref="ContentMetaData.PublishedDate"/> of a result.
  /// </summary>
  public Boolean CanReportPublishedDate { get; }

  /// <summary>
  /// Specifies whether the search provider results include <see cref="ContentMetaData.Categories"/>.
  /// </summary>
  public Boolean CanReportCategories { get; }

  /// <summary>
  /// Specifies whether the search provider results include a <see cref="ContentMetaData.Type"/>.
  /// </summary>
  public Boolean CanReportType { get; }

  /// <summary>
  /// Specifies whether the search provider can detected matched terms to populate <see cref="SearchResult.MatchedTerms" />.
  /// </summary>
  public Boolean CanReportMatchedTerms { get; }
}
