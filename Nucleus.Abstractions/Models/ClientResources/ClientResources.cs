using System;
using System.Collections.Generic;

namespace Nucleus.Abstractions.Models.ClientResources;

/// <summary>
/// Class used to store a list of client resources (scripts, stylesheets and web assemblies) which are used by a page.
/// </summary>
/// <remarks>
/// This class is used by the AddScript, AddStyle and AddWebAssembly Html Helpers in Nucleus.ViewFeatures.
/// </remarks>
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

  /// <summary>
  /// A list of Web Assemblies to dynamically load.
  /// </summary>
  public Dictionary<string, WebAssembly> WebAssemblies { get; } = new(StringComparer.OrdinalIgnoreCase);
}
