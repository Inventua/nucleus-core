// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a copy from https://github.com/dotnet/aspnetcore/blob/main/src/FileProviders/Embedded/src/ with a new 
// constructor which allows the caller to specify an extension name, which is used when calling ManifestParser.Parse().
// The other constructors are redundant and have been removed.

using System;
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
internal class NucleusExtensionManifestEmbeddedFileProvider : IFileProvider
{
  private readonly DateTimeOffset _lastModified;

  /////// <summary>
  /////// Initializes a new instance of <see cref="ExtensionManifestEmbeddedFileProvider"/>.
  /////// </summary>
  /////// <param name="assembly">The assembly containing the embedded files.</param>
  ////public ExtensionManifestEmbeddedFileProvider(Assembly assembly)
  ////    : this(assembly, ManifestParser.Parse(assembly), ResolveLastModified(assembly)) { }

  /////// <summary>
  /////// Initializes a new instance of <see cref="ExtensionManifestEmbeddedFileProvider"/>.
  /////// </summary>
  /////// <param name="assembly">The assembly containing the embedded files.</param>
  /////// <param name="root">The relative path from the root of the manifest to use as root for the provider.</param>
  ////public ExtensionManifestEmbeddedFileProvider(Assembly assembly, string root)
  ////    : this(assembly, root, ResolveLastModified(assembly))
  ////{
  ////}

  ///// <summary>
  ///// Initializes a new instance of <see cref="ExtensionManifestEmbeddedFileProvider"/>.
  ///// </summary>
  ///// <param name="assembly">The assembly containing the embedded files.</param>
  ///// <param name="root">The relative path from the root of the manifest to use as root for the provider.</param>
  ///// <param name="lastModified">The LastModified date to use on the <see cref="IFileInfo"/> instances
  ///// returned by this <see cref="IFileProvider"/>.</param>
  //public ExtensionManifestEmbeddedFileProvider(Assembly assembly, string root, DateTimeOffset lastModified)
  //    : this(assembly, ManifestParser.Parse(assembly).Scope(root), lastModified)
  //{
  //}

  /// <summary>
  /// Initializes a new instance of <see cref="ExtensionManifestEmbeddedFileProvider"/>.
  /// </summary>
  /// <param name="assembly">The assembly containing the embedded files.</param>
  /// <param name="rootDirectoryName">Specifies a prefix to apply to embedded resources.</param>
  /// <param name="root">The relative path from the root of the manifest to use as root for the provider.</param>
  /// returned by this <see cref="IFileProvider"/>.</param>
  /// <remarks>
  /// Use this overload to create an embedded file provider which applies a prefix to embedded resource paths.
  /// </remarks>
  public NucleusExtensionManifestEmbeddedFileProvider(Assembly assembly, string extensionName)
         : this(assembly, ManifestParser.Parse(assembly, extensionName), ResolveLastModified(assembly))
  {
  }

  ///// <summary>
  ///// Initializes a new instance of <see cref="ExtensionManifestEmbeddedFileProvider"/>.
  ///// </summary>
  ///// <param name="assembly">The assembly containing the embedded files.</param>
  ///// <param name="root">The relative path from the root of the manifest to use as root for the provider.</param>
  ///// <param name="manifestName">The name of the embedded resource containing the manifest.</param>
  ///// <param name="lastModified">The LastModified date to use on the <see cref="IFileInfo"/> instances
  ///// returned by this <see cref="IFileProvider"/>.</param>
  //public ExtensionManifestEmbeddedFileProvider(Assembly assembly, string root, string manifestName, DateTimeOffset lastModified)
  //      : this(assembly, ManifestParser.Parse(assembly, "", manifestName).Scope(root), lastModified)
  //  {
  //  }

  internal NucleusExtensionManifestEmbeddedFileProvider(Assembly assembly, EmbeddedFilesManifest manifest, DateTimeOffset lastModified)
  {
    ArgumentNullThrowHelper.ThrowIfNull(assembly);
    ArgumentNullThrowHelper.ThrowIfNull(manifest);

    Assembly = assembly;
    Manifest = manifest;
    _lastModified = lastModified;
  }

  /// <summary>
  /// Gets the <see cref="Assembly"/> for this provider.
  /// </summary>
  public Assembly Assembly { get; }

  internal EmbeddedFilesManifest Manifest { get; }

  /// <inheritdoc />
  public IDirectoryContents GetDirectoryContents(string subpath)
  {
    var entry = Manifest.ResolveEntry(subpath);
    if (entry == null || entry == ManifestEntry.UnknownPath)
    {
      return NotFoundDirectoryContents.Singleton;
    }

    if (!(entry is ManifestDirectory directory))
    {
      return NotFoundDirectoryContents.Singleton;
    }

    return new ManifestDirectoryInfo(Assembly, directory, _lastModified);
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
        return new ManifestFileInfo(Assembly, f, _lastModified);
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

  [UnconditionalSuppressMessage("SingleFile", "IL3000:Assembly.Location",
      Justification = "The code handles if the Assembly.Location is empty. Workaround https://github.com/dotnet/runtime/issues/83607")]
  private static DateTimeOffset ResolveLastModified(Assembly assembly)
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