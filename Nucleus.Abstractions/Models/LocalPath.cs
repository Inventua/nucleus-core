﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Provides an object representation of a the part of the request path that was not resolved to a page, and easy access to the parts of the path.
	/// </summary>
  /// <remarks>
  /// Nucleus extensions do not create this object directly. Use the <seealso cref="Context">Context</seealso>.
  /// <seealso cref="Context.LocalPath">LocalPath</seealso> property to get a LocalPath instance.
  /// </remarks>
  /// <example>
  /// if (this.Context.LocalPath.Segments.Length == 1 &amp;&amp; Guid.TryParse(this.Context.LocalPath.Segments.First(), out Guid productId))
  /// {
  ///   // if id is specified in the url, and is a guid, load content by id
  ///   // ...
  /// }
/// </example>
public class LocalPath
	{
		/// <summary>
		/// Initializes a new instance of the LocalPath class.
		/// </summary>
		/// <param name="path"></param>
    /// <remarks>
    /// Nucleus extensions do not use this contructor.  The LocalPath property of the <seealso cref="Context"/> class provides access to the local path.
    /// </remarks>
		public LocalPath(string path)
		{
			this.FullPath = path;
		}

		/// <summary>
		/// Returns whether this LocalPath instance has a value.
		/// </summary>
		public Boolean HasValue
		{
			get
			{
				return !String.IsNullOrEmpty(this.FullPath);
			}
		}
		/// <summary>
		/// Gets a string representation for the specified Uri path (returns AbsolutePath).
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return this.FullPath;
		}

		/// <summary>
		/// Return the local path
		/// </summary>
		public string FullPath { get; }

		/// <summary>
		/// Gets the local path part of the specified absolute path (the part that does not contain the query)
		/// </summary>
		public string Path
		{
			get
			{
				return this.FullPath.Split(new[] { '?' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
			}
		}

		/// <summary>
		/// Gets an array containing the path segments that make up the local path.
		/// </summary>
		public string[] Segments 
		{ 
			get
			{
				return this.Path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
			}
		}


	}
}
