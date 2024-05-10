using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.StaticResources;

/// <summary>
/// Represents a stylesheet which is added as a link element in the page output.
/// </summary>
public class StyleSheet
{
  /// <summary>
  /// Script or related component version, used in the rendered Url for cache busting.
  /// </summary>
  public string Version { get; set; }

  /// <summary>
  /// Specifies whether to render a 'defer' attribute.
  /// </summary>
  public Boolean Defer { get; set; }

  /// <summary>
  /// Specifies whether the stylesheet loads other related resources using a relative path.
  /// </summary>
  public Boolean IsDynamic { get; set; }

  /// <summary>
  /// Script path.
  /// </summary>
  public string Path { get; set; }

  /// <summary>
  /// Specifies the rendering order of stylesheet links.
  /// </summary>
  public int SortIndex { get; set; }
}
