// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a copy from https://github.com/dotnet/aspnetcore/blob/main/src/FileProviders/Embedded/src/ with a change to the constructor
// to specify the extension name.
using System;
using Microsoft.Extensions.Primitives;

namespace Nucleus.Core.FileProviders.Manifest;

internal sealed class ManifestSinkDirectory : ManifestDirectory
{
  public ManifestSinkDirectory(string extensionName)
      : base(name: extensionName, children: Array.Empty<ManifestEntry>())
  {
    SetParent(this);
    Children = new[] { this };
  }

  private ManifestSinkDirectory()
     : base(name: String.Empty, children: Array.Empty<ManifestEntry>())
  {
    SetParent(this);
    Children = new[] { this };
  }

  public static ManifestDirectory Instance { get; } = new ManifestSinkDirectory();

  public override ManifestEntry Traverse(StringSegment segment) => this;
}
