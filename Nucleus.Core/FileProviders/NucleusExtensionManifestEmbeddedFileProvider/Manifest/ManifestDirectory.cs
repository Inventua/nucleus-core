// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a copy from https://github.com/dotnet/aspnetcore/blob/main/src/FileProviders/Embedded/src/.  See NucleusExtensionManifestEmbeddedFileProvider.cs
// for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using global::Microsoft.Extensions.Primitives;

namespace Nucleus.Core.FileProviders.Manifest;

internal class ManifestDirectory : ManifestEntry
{
  protected ManifestDirectory(string name, Assembly assembly, ManifestEntry[] children, DateTimeOffset lastModified)
      : base(name, assembly, lastModified)
  {
    ArgumentNullThrowHelper.ThrowIfNull(children);

    this.Children = children;
  }

  public IReadOnlyList<ManifestEntry> Children { get; protected set; }

  public override ManifestEntry Traverse(StringSegment segment)
  {
    if (segment.Equals(".", StringComparison.Ordinal) || segment.Equals(this.Name, StringComparison.Ordinal))
    {
      return this;
    }

    if (segment.Equals("..", StringComparison.Ordinal))
    {
      return Parent ?? UnknownPath;
    }

    foreach (var child in Children)
    {
      if (segment.Equals(child.Name, StringComparison.OrdinalIgnoreCase))
      {
        return child;
      }
    }

    return UnknownPath;
  }

  public static ManifestDirectory CreateDirectory(string name, Assembly assembly, ManifestEntry[] children, DateTimeOffset lastModified)
  {
    ArgumentThrowHelper.ThrowIfNullOrWhiteSpace(name);
    ArgumentNullThrowHelper.ThrowIfNull(assembly);
    ArgumentNullThrowHelper.ThrowIfNull(children);

    var result = new ManifestDirectory(name, assembly, children, lastModified);
    ValidateChildrenAndSetParent(children, result);

    return result;
  }

  public static ManifestRootDirectory CreateRootDirectory(string rootDirectoryName, ManifestEntry[] children)
  {
    ArgumentNullThrowHelper.ThrowIfNull(children);

    var result = new ManifestRootDirectory(rootDirectoryName, null, children, DateTimeOffset.MaxValue);
    ValidateChildrenAndSetParent(children, result);

    return result;
  }

  internal static void ValidateChildrenAndSetParent(ManifestEntry[] children, ManifestDirectory parent)
  {
    foreach (var child in children)
    {
      if (child == UnknownPath)
      {
        throw new InvalidOperationException($"Invalid entry type '{nameof(ManifestSinkDirectory)}'");
      }

      if (child is ManifestRootDirectory)
      {
        throw new InvalidOperationException($"Can't add a root folder as a child");
      }

      child.SetParent(parent);
    }
  }

  private ManifestEntry[] CopyChildren()
  {
    var list = new List<ManifestEntry>(Children.Count);
    for (var i = 0; i < Children.Count; i++)
    {
      var child = Children[i];
      switch (child)
      {
        case ManifestSinkDirectory:
        case ManifestRootDirectory:
          throw new InvalidOperationException("Unexpected manifest node.");
        case ManifestDirectory d:
          var grandChildren = d.CopyChildren();
          var newDirectory = CreateDirectory(d.Name, this.Assembly, grandChildren, this.LastModified);
          list.Add(newDirectory);
          break;
        case ManifestFile f:
          var file = new ManifestFile(f.Name, f.Assembly,f.LastModified, f.ResourcePath);
          list.Add(file);
          break;
        default:
          throw new InvalidOperationException("Unexpected manifest node.");
      }
    }

    return list.ToArray();
  }
}