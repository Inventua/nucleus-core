// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// This is a (slightly) modified version of the (internal) PathUtils class from the .net core PhysicalFileProvider - source code from
// https://github.com/aspnet/FileSystem/blob/master/src/FS.Physical/Internal/PathUtils.cs. 

using System.IO;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Nucleus.Core
{
  /// <summary>
  /// Utilities used to validate paths and filenames.
  /// </summary>
  /// <remarks>
  /// This is a (slightly) modified version of the (internal) PathUtils class from the .net core PhysicalFileProvider - source code from
  /// https://github.com/aspnet/FileSystem/blob/master/src/FS.Physical/Internal/PathUtils.cs. 
  /// </remarks>
  internal static class PathUtils
  {
    private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars()
        .Where(c => c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar).ToArray();

    private static readonly char[] _invalidFilterChars = _invalidFileNameChars
        .Where(c => c != '*' && c != '|' && c != '?').ToArray();

    private static readonly char[] _pathSeparators = new[]
        {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar};

    /// <summary>
    /// Returns a true/false value indicating whether the specified path has any characters which are not valid in a folder name.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    internal static bool HasInvalidPathChars(string path)
    {
      if (path == null) return false;
      return path.IndexOfAny(_invalidFileNameChars) != -1;
    }

    /// <summary>
    /// Returns true/false value indicating whether the specified path contains any characters which are not valid in a filename.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    internal static bool HasInvalidFileChars(string path)
    {
      if (path == null) return false;
      return path.IndexOfAny(_invalidFileNameChars) != -1 || path.IndexOfAny(_pathSeparators) != -1;
    }

    //internal static bool HasInvalidFilterChars(string path)
    //{
    //  if (path == null) return false;
    //  return path.IndexOfAny(_invalidFilterChars) != -1;
    //}

    /// <summary>
    /// Check the specified path and append a trailing slash if it does not already have one.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    internal static string EnsureTrailingSlash(string path)
    {
      if (!string.IsNullOrEmpty(path) &&
          path[path.Length - 1] != Path.DirectorySeparatorChar)
      {
        return path + Path.DirectorySeparatorChar;
      }

      return path;
    }

    /// <summary>
    /// Returns a true/false value indicating whether the specified path attempts to navigate the web root.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    internal static bool PathNavigatesAboveRoot(string path)
    {
      if (path == null) return false;
      var tokenizer = new StringTokenizer(path, _pathSeparators);
      var depth = 0;

      foreach (var segment in tokenizer)
      {
        if (segment.Equals(".") || segment.Equals(""))
        {
          continue;
        }
        else if (segment.Equals(".."))
        {
          depth--;

          if (depth == -1)
          {
            return true;
          }
        }
        else
        {
          depth++;
        }
      }

      return false;
    }
  }
}