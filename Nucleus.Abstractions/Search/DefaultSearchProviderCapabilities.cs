namespace Nucleus.Abstractions.Search;

/// <summary>
/// Default implementation of <see cref="ISearchProviderCapabilities"/> which reports that all capabilities are available.
/// </summary>
public class DefaultSearchProviderCapabilities : ISearchProviderCapabilities
{
  /// <inheritdoc />
  public int MaximumPageSize { get; init; } = 250;

  /// <inheritdoc />
  public int MaximumSuggestions { get; init; } = 100;

  /// <inheritdoc />
  public bool CanFilterByScope { get; init; } = true;

  /// <inheritdoc />
  public bool CanReportSize { get; init; } = true;

  /// <inheritdoc />
  public bool CanReportScore { get; init; } = true;

  /// <inheritdoc />
  public bool CanReportPublishedDate { get; init; } = true;

  /// <inheritdoc />
  public bool CanReportCategories { get; init; } = true;

  /// <inheritdoc />
  public bool CanReportType { get; init; } = true;
}
