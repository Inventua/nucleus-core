namespace Nucleus.Abstractions.Models.ClientResources;

/// <summary>
/// Represents a web assembly which is registered to be dynamically loaded by the browser.
/// </summary>
public class WebAssembly
{
  /// <summary>
  /// Script or related component version, used in the rendered Url for cache busting.
  /// </summary>
  public string Version { get; set; }

  /// <summary>
  /// Script path.
  /// </summary>
  public string Path { get; set; }
}
