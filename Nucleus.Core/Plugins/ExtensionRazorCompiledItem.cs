using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Hosting;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Core.Plugins;

/// <summary>
/// Implementation of RazorCompiledItem which adds an Extensions/extension-folder prefix to the identifier property during construction, so 
/// that the "path" of compiled razor items matches the path which is expected by Plugins.ExtensionViewLocationExpander.
/// </summary>
/// <remarks>
/// RazorCompiledItem is abstract, and DefaultRazorCompiledItem is protected, so we must inherit and override all of the 
/// methods of RazorCompiledItem.
/// </remarks>
internal class ExtensionRazorCompiledItem : RazorCompiledItem
{
  internal ExtensionRazorCompiledItem(RazorCompiledItem original, string extensionFolder)
  {
    // Copy the original RazorCompiledItem instances, but add an "/Extensions/extension-folder" prefix to the Identifer property.
    // This line of code is the entire point of the RazorCompiledItemLoader, and CompiledExtensionRazorApplicationPart classes, along
    // with the code in PluginExtensions.AddCompiledRazorViews which creates instances of CompiledExtensionRazorApplicationPart.
    this.Identifier = $"/{FolderOptions.EXTENSIONS_FOLDER}/{extensionFolder}{original.Identifier}";

    this.Kind = original.Kind;
    this.Metadata = original.Metadata;
    this.Type = original.Type;
  }

  public override string Identifier { get; }

  public override string Kind { get; }

  public override IReadOnlyList<object> Metadata { get; }

  public override Type Type { get; }
}

