// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a copy from https://github.com/dotnet/aspnetcore/blob/main/src/FileProviders/Embedded/src/.  See NucleusExtensionManifestEmbeddedFileProvider.cs
// for more information.

using System;
using Microsoft.Extensions.Primitives;

#nullable enable

namespace Nucleus.Core.FileProviders.Manifest;

/// <summary>
/// Base class for ManifestDirectory and ManifestFile.
/// </summary>
internal abstract class ManifestEntry
{
  public System.Reflection.Assembly Assembly { get; }
  public DateTimeOffset LastModified { get; }

  public ManifestEntry(string name, System.Reflection.Assembly assembly, DateTimeOffset lastModified )
  {
    this.Name = name;
    this.Assembly = assembly;
    this.LastModified = lastModified;
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

