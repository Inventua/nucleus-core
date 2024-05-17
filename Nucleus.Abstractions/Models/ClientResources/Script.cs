using System;

namespace Nucleus.Abstractions.Models.ClientResources;

/// <summary>
/// Represents a script which will be included in the page output.
/// </summary>
public class Script
{
  /// <summary>
  /// Script or related component version, used in the rendered Url for cache busting.
  /// </summary>
  public string Version { get; init; }

  /// <summary>
  /// Specifies whether to render a 'async' attribute.
  /// </summary>
  public Boolean IsAsync { get; init; }

  /// <summary>
  /// Specifies whether the script dynamically loads other related scripts.
  /// </summary>
  /// <remarks>
  /// This value is currently not used, but may be used in future to ensure that relative paths are preserved so that dynamic script loading with
  /// relative paths works correctly.
  /// </remarks>
  public Boolean IsDynamic { get; init; }

  /// <summary>
  /// Script path.
  /// </summary>
  public string Path { get; init; }

  /// <summary>
  /// Specifies the rendering order of script elements.
  /// </summary>
  public int Order { get; init; } = 0;

  /// <summary>
  /// Indicates whether the script belongs to an extension.
  /// </summary>
  public Boolean IsExtensionScript { get; init; } = false;
}