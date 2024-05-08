// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Nucleus.Core.FileProviders.Manifest;

namespace Nucleus.Core.FileProviders;

/// <summary>
/// An embedded file provider that uses a manifest compiled in the assembly to reconstruct the original paths of the embedded 
/// files when they were embedded into the assembly.  This class has the same functionality as the .net core ManifestEmbeddedFileProvider
/// but creates a document tree with a root entry named "Extensions", which contains a child entry for the specified extension, which has 
/// Children elements which are the embedded files.
/// </summary>

// This is a modified copy from https://github.com/dotnet/aspnetcore/blob/main/src/FileProviders/Embedded/src/..
internal class NucleusExtensionManifestEmbeddedFileProvider : IFileProvider
{
  public NucleusExtensionManifestEmbeddedFileProvider(IEnumerable<Assembly> assemblies)
    : this(ManifestParser.Parse(assemblies))
  {
  }

  internal NucleusExtensionManifestEmbeddedFileProvider(EmbeddedFilesManifest manifest)
  {
    ArgumentNullThrowHelper.ThrowIfNull(manifest);

    Manifest = manifest;
  }

  internal EmbeddedFilesManifest Manifest { get; }

  /// <inheritdoc />
  public IDirectoryContents GetDirectoryContents(string subpath)
  {
    var entry = Manifest.ResolveEntry(subpath);
    if (entry == null || entry == ManifestEntry.UnknownPath)
    {
      return NotFoundDirectoryContents.Singleton;
    }

    if (entry is not ManifestDirectory directory)
    {
      return NotFoundDirectoryContents.Singleton;
    }
    
    return new ManifestDirectoryInfo(directory);
  }

  /// <inheritdoc />
  public IFileInfo GetFileInfo(string subpath)
  {
    var entry = Manifest.ResolveEntry(subpath);
    switch (entry)
    {
      case null:
        return new NotFoundFileInfo(subpath);
      case ManifestFile f:
        return new ManifestFileInfo(f);
      case ManifestDirectory d when d != ManifestEntry.UnknownPath:
        return new NotFoundFileInfo(d.Name);
    }

    return new NotFoundFileInfo(subpath);
  }

  /// <inheritdoc />
  public IChangeToken Watch(string filter)
  {
    ArgumentNullThrowHelper.ThrowIfNull(filter);

    return NullChangeToken.Singleton;
  }

  internal static DateTimeOffset ResolveLastModified(Assembly assembly)
  {
    var result = DateTimeOffset.UtcNow;

    var assemblyLocation = assembly.Location;
    if (!string.IsNullOrEmpty(assemblyLocation))
    {
      try
      {
        result = File.GetLastWriteTimeUtc(assemblyLocation);
      }
      catch (PathTooLongException)
      {
      }
      catch (UnauthorizedAccessException)
      {
      }
    }

    return result;
  }
}