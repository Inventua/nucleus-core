using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.Hosting;

namespace Nucleus.Core.Plugins;

/// <summary>
/// This class is used by PluginExtensions.AddCompiledRazorViews to create ApplicationPart instances which override the CompiledItems() property
/// in order to use our Plugins.RazorCompiledItemLoader implementation, which adds an Extensions/extension-name prefix to the identifier property 
/// of each RazorCompiledItem returned, so that the "path" of compiled razor items matches the path which is expected by
/// Plugins.ExtensionViewLocationExpander.
/// </summary>
internal class ExtensionCompiledRazorApplicationPart : CompiledRazorAssemblyPart, IRazorCompiledItemProvider
{
  public ExtensionCompiledRazorApplicationPart(Assembly assembly) : base(assembly) { }
  
  IEnumerable<RazorCompiledItem> IRazorCompiledItemProvider.CompiledItems
  {
    get
    {
      RazorCompiledItemLoader loader = new Plugins.ExtensionRazorCompiledItemLoader();
      return loader.LoadItems(this.Assembly);
    }
  }
}
