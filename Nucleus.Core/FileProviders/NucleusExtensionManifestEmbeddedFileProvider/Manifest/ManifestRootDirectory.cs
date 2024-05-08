// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a copy from https://github.com/dotnet/aspnetcore/blob/main/src/FileProviders/Embedded/src/.  See NucleusExtensionManifestEmbeddedFileProvider.cs
// for more information.

using System;
using System.Reflection;

namespace Nucleus.Core.FileProviders.Manifest;

internal sealed class ManifestRootDirectory : ManifestDirectory
{
  public ManifestRootDirectory(string extensionName, Assembly assembly, ManifestEntry[] children, DateTimeOffset lastModified)
      : base(extensionName, assembly, children, lastModified)
  {
    SetParent(new ManifestSinkDirectory(extensionName, assembly, lastModified));
  }
}