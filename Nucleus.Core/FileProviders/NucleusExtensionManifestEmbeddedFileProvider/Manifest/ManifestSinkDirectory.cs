// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a copy from https://github.com/dotnet/aspnetcore/blob/main/src/FileProviders/Embedded/src/.  See NucleusExtensionManifestEmbeddedFileProvider.cs
// for more information.

using System;
using Microsoft.Extensions.Primitives;

namespace Nucleus.Core.FileProviders.Manifest;

internal sealed class ManifestSinkDirectory : ManifestDirectory
{
  public ManifestSinkDirectory(string extensionName, System.Reflection.Assembly assembly, DateTimeOffset lastModified)
      : base(name: extensionName, assembly: assembly, children: [], lastModified: lastModified)
  {
    base.SetParent(this);
    this.Children = [ this ];
  }

  private ManifestSinkDirectory()
     : base(name: String.Empty, assembly: null, children: [], lastModified: DateTimeOffset.MaxValue)
  {
    base.SetParent(this);
    this.Children = [ this ];
  }

  public static ManifestDirectory Instance { get; } = new ManifestSinkDirectory();

  public override ManifestEntry Traverse(StringSegment segment) => this;
}
