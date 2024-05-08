// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a copy from https://github.com/dotnet/aspnetcore/blob/main/src/FileProviders/Embedded/src/ with a change to the constructor
// to specofy the extension name.  

namespace Nucleus.Core.FileProviders.Manifest;

internal sealed class ManifestRootDirectory : ManifestDirectory
{
  public ManifestRootDirectory(string extensionName, ManifestEntry[] children)
      : base(name: extensionName, children: children)
  {
    SetParent(new ManifestSinkDirectory(extensionName));
  }

  public override ManifestDirectory ToRootDirectory() => this;
}