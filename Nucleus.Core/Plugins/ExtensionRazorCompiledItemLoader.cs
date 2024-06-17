using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Hosting;

namespace Nucleus.Core.Plugins;

/// <summary>
/// RazorCompiledItemLoader implementation which overrides LoadItems to return ExtensionRazorCompiledItem instances.   This has the end result of 
/// adding an /Extensions/extension-folder prefix to the identifier property of each RazorCompiledItem returned, so that the "path" of compiled razor
/// items matches the path which is expected by Plugins.ExtensionViewLocationExpander.  See comments in ExtensionRazorCompiledItem.cs.
/// </summary>
internal class ExtensionRazorCompiledItemLoader : RazorCompiledItemLoader
{
  public override IReadOnlyList<RazorCompiledItem> LoadItems(Assembly assembly)
  {
    string assemblyLocation = assembly.Location;
    string extensionFolder = AssemblyLoader.GetExtensionFolderName(assemblyLocation);

    // if the assembly is not located in /Extensions, call the base implementation instead of returning ExtensionRazorCompiledItem
    // instances.  The code in AddCompiledRazorViews.AddCompiledRazorViews contains the same check, so extensionFolder should be non-null
    // in all cases, unless something goes wrong.

    // for Nucleus extensions, return ExtensionRazorCompiledItem instances, which modify the Identifier property to conform with
    // the expected location of extension views.
    return String.IsNullOrEmpty(extensionFolder) ? base.LoadItems(assembly) : LoadExtensionItems(base.LoadItems(assembly), extensionFolder);               
  }

  /// <summary>
  /// Convert the elements of the <paramref name="items"/> parameter to ExtensionRazorCompiledItem instances and return them.
  /// </summary>
  /// <param name="items"></param>
  /// <param name="extensionFolder"></param>
  /// <returns></returns>
  private static List<RazorCompiledItem> LoadExtensionItems(IReadOnlyList<RazorCompiledItem> items, string extensionFolder)
  {
    return items.Select(item => new ExtensionRazorCompiledItem(item, extensionFolder)).ToList<RazorCompiledItem>();
  }
}