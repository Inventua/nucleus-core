// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a copy from https://github.com/dotnet/aspnetcore/blob/main/src/FileProviders/Embedded/src/.  See NucleusExtensionManifestEmbeddedFileProvider.cs
// for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.FileProviders;

#nullable enable

namespace Nucleus.Core.FileProviders.Manifest;

internal sealed class ManifestDirectoryInfo : IFileInfo, IDirectoryContents
{
  private IFileInfo[]? _entries;

  public ManifestDirectoryInfo(ManifestDirectory directory)
  {
    ArgumentNullThrowHelper.ThrowIfNull(directory);

    this.Directory = directory;
    this.LastModified = directory.LastModified;
  }

  public bool Exists => true;

  public long Length => -1;

  public string? PhysicalPath => null;

  public string Name => Directory.Name;

  public DateTimeOffset LastModified { get; }

  public bool IsDirectory => true;

  private ManifestDirectory Directory { get; }

  public Stream CreateReadStream() => throw new InvalidOperationException("Cannot create a stream for a directory.");

  public IEnumerator<IFileInfo> GetEnumerator()
  {
    return EnsureEntries().GetEnumerator();

    IEnumerable<IFileInfo> EnsureEntries() => _entries ??= ResolveEntries().ToArray();

    IEnumerable<IFileInfo> ResolveEntries()
    {
      if (Directory == ManifestEntry.UnknownPath)
      {
        return [];
      }

      var entries = new List<IFileInfo>();

      foreach (var entry in Directory.Children)
      {
        IFileInfo fileInfo = entry switch
        {
          ManifestFile file => new ManifestFileInfo(file),
          ManifestDirectory directory => new ManifestDirectoryInfo( directory),
          _ => throw new InvalidOperationException("Unknown entry type")
        };

        entries.Add(fileInfo);
      }

      return entries;
    }
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
