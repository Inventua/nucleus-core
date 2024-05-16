// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a copy from https://github.com/dotnet/aspnetcore/blob/main/src/FileProviders/Embedded/src/ with no changes.  It is
// copied because the .net class is marked internal.
using Microsoft.Extensions.Primitives;

namespace Nucleus.Core.FileProviders.Manifest;

internal sealed class ManifestFile : ManifestEntry
{
  public ManifestFile(string name, string resourcePath)
      : base(name)
  {
    ArgumentThrowHelper.ThrowIfNullOrWhiteSpace(name);
    ArgumentThrowHelper.ThrowIfNullOrWhiteSpace(resourcePath);

    ResourcePath = resourcePath;
  }

  public string ResourcePath { get; }

  public override ManifestEntry Traverse(StringSegment segment) => UnknownPath;
}

