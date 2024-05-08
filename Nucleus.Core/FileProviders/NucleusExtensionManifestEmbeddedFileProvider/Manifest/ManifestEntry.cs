﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a copy from https://github.com/dotnet/aspnetcore/blob/main/src/FileProviders/Embedded/src/ with no changes.  It is
// copied because the .net class is marked internal.
using System;
using Microsoft.Extensions.Primitives;

#nullable enable

namespace Nucleus.Core.FileProviders.Manifest;

internal abstract class ManifestEntry
{
  public ManifestEntry(string name)
  {
    Name = name;
  }

  public ManifestEntry? Parent { get; private set; }

  public string Name { get; }

  public static ManifestEntry UnknownPath { get; } = ManifestSinkDirectory.Instance;

  protected internal virtual void SetParent(ManifestDirectory directory)
  {
    if (Parent != null)
    {
      throw new InvalidOperationException("Directory already has a parent.");
    }

    Parent = directory;
  }

  public abstract ManifestEntry Traverse(StringSegment segment);
}

