// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a copy from https://github.com/dotnet/aspnetcore/blob/main/src/FileProviders/Embedded/src/.  See NucleusExtensionManifestEmbeddedFileProvider.cs
// for more information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.FileProviders;

#nullable enable

namespace Nucleus.Core.FileProviders.Manifest;

internal sealed class ManifestFileInfo : IFileInfo
{
  private long? _length;

  public ManifestFileInfo(ManifestFile file)
  {
    ArgumentNullThrowHelper.ThrowIfNull(file);

    this.Assembly = file.Assembly;
    this.ManifestFile = file;
    this.LastModified = file.LastModified;
  }

  public Assembly Assembly { get; }

  public ManifestFile ManifestFile { get; }

  public bool Exists => true;

  public long Length => EnsureLength();

  public string? PhysicalPath => null;

  public string Name => ManifestFile.Name;

  public DateTimeOffset LastModified { get; }

  public bool IsDirectory => false;

  private long EnsureLength()
  {
    if (_length == null)
    {
      using var stream = GetManifestResourceStream();
      _length = stream.Length;
    }

    return _length.Value;
  }

  public Stream CreateReadStream()
  {
    var stream = GetManifestResourceStream();
    if (!_length.HasValue)
    {
      _length = stream.Length;
    }

    return stream;
  }

  private Stream GetManifestResourceStream()
  {
    var stream = Assembly.GetManifestResourceStream(ManifestFile.ResourcePath);
    if (stream == null)
    {
      throw new InvalidOperationException($"Couldn't get resource at '{ManifestFile.ResourcePath}'.");
    }

    return stream;
  }
}
