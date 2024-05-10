using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.StaticResources;

/// <summary>
/// Class used to store a list of scripts and stylesheets to be added to the page output.
/// </summary>
public class ClientResources
{
  /// <summary>
  /// Key for page resources object in HttpContext.Items
  /// </summary>
  public const string ITEMS_KEY = "PAGE_RESOURCES";

  /// <summary>
  /// A list of scripts to add to the current page output.
  /// </summary>
  public Dictionary<string, Script> Scripts { get; } = new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// A list of stylesheets to add to the current page output.
  /// </summary>
  public Dictionary<string, StyleSheet> StyleSheets { get; } = new(StringComparer.OrdinalIgnoreCase);

}
