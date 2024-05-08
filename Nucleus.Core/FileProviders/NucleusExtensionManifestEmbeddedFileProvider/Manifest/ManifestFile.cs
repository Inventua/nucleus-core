// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a copy from https://github.com/dotnet/aspnetcore/blob/main/src/FileProviders/Embedded/src/.  See NucleusExtensionManifestEmbeddedFileProvider.cs
// for more information.

using System;
using Microsoft.Extensions.Primitives;

namespace Nucleus.Core.FileProviders.Manifest;

internal sealed class ManifestFile : ManifestEntry
{
  public ManifestFile(string name, System.Reflection.Assembly assembly, DateTimeOffset lastModified, string resourcePath)
      : base(name, assembly, lastModified)
  {
    ArgumentThrowHelper.ThrowIfNullOrWhiteSpace(name);
    ArgumentThrowHelper.ThrowIfNullOrWhiteSpace(resourcePath);

    this.ResourcePath = resourcePath;
  }

  public string ResourcePath { get; }

  public override ManifestEntry Traverse(StringSegment segment) => UnknownPath;
}

