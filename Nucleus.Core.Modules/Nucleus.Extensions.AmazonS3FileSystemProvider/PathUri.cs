using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Globalization;

namespace Nucleus.Extensions.AmazonS3FileSystemProvider
{
	/// <summary>
	/// Represents an S3/OpenStack object path and provides parsing and access to parts of the path.
	/// </summary>
	[DefaultBindingProperty("Path")]
	public class PathUri
	{
		/// <summary>
		/// Path part delimiter
		/// </summary>
		public const char PATH_DELIMITER = '/';

		private static char[] DirectorySeparators = new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar };

		/// <summary>
		/// Path type
		/// </summary>
		public enum PathUriTypes
		{
			/// <summary>
			/// The path is the root, which contains buckets (containers).
			/// </summary>
			Root,
			/// <summary>
			/// The path is a bucket (container).
			/// </summary>
			Bucket,
			/// <summary>
			/// The path is a folder.
			/// </summary>
			Folder,
			/// <summary>
			/// The path is a file.
			/// </summary>
			File
		}

		/// <value>
		/// Provider root path (base path).
		/// </value>
		public string RootPath { get; private set; }

		/// <value>
		/// Bucket (container) name component of the path.
		/// </value>
		public string BucketName { get; private set; }

		/// <value>
		/// Key component of the path.
		/// </value>
		public string Key { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PathUri()
		{
			this.BucketName = string.Empty;
			this.Key = string.Empty;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="BucketName">Bucket (container) name</param>
		/// <param name="Key">Item key</param>
		//public PathUri(string rootPath, string bucketName, string key)
		//{
		//	this.RootPath = RemoveDelimiter(rootPath);
		//	this.BucketName = bucketName;
		//	this.Key = key;
		//}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rootPath"></param>
		/// <param name="path">String representation of the path</param>
		public PathUri(string rootPath, string path) : this(rootPath, path, false) {	}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rootPath"></param>
		/// <param name="path">String representation of the path</param>
		/// <param name="isFolder"></param>
		public PathUri(string rootPath, string path, Boolean isFolder)
		{
			this.RootPath = RemoveDelimiter(rootPath);
			Parse(isFolder && !String.IsNullOrEmpty(path) ? AddDelimiter(path) : path ?? "");
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rootPath"></param>
		/// <param name="path">String representation of the path</param>
		/// <param name="itemname"></param>
		/// <param name="isFolder"></param>
		public PathUri(string rootPath, string path, string itemName, Boolean isFolder) : this(rootPath, Combine(path, itemName), isFolder) { }
		
		private static string Combine(string path, string item)
		{
			if (!String.IsNullOrEmpty(path) && !String.IsNullOrEmpty(item))
			{
				return AddDelimiter(path) + item;
			}
			return path + item;
		}

		private void Parse(string path)
		{
			string fullPath;

			if (path == PATH_DELIMITER.ToString())
			{  
				fullPath = this.RootPath + path;
			}
			else
			{
				fullPath = this.RootPath + PATH_DELIMITER + path;
			}	

			int keyPosition;
			string[] pathParts = fullPath.Split(PATH_DELIMITER, StringSplitOptions.RemoveEmptyEntries);

			if (pathParts.Length > 0)
			{
				this.BucketName = pathParts[0];

				keyPosition = fullPath.IndexOf(this.BucketName) + this.BucketName.Length + 1;
				if (fullPath.Length > keyPosition)
				{
					this.Key = fullPath.Substring(keyPosition);
				}
				else
				{
					this.Key = "";
				}
			}
			else
			{
				this.BucketName = PATH_DELIMITER.ToString();
			}
		}

		/// <summary>
		/// Gets the path type.
		/// </summary>
		public PathUriTypes PathUriType
		{
			get
			{
				if (String.IsNullOrEmpty(this.Key) && (String.IsNullOrEmpty(this.BucketName) || this.BucketName == PATH_DELIMITER.ToString()))
				{
					return PathUriTypes.Root;
				}
				else if (!String.IsNullOrEmpty(this.BucketName) && string.IsNullOrEmpty(this.Key))
				{
					return PathUriTypes.Bucket;
				}
				else
				{
					if (this.Key.EndsWith(PATH_DELIMITER))
					{
						return PathUriTypes.Folder;
					}
					else
					{
						return PathUriTypes.File;
					}
				}
			}
		}

		/// <summary>
		/// Gets the file name component of the path.  
		/// </summary>
		/// <remarks>
		/// This method returns a result for paths of type File.  Other path types return an empty string.
		/// </remarks>
		public string FileName
		{
			get
			{
				if (this.PathUriType == PathUriTypes.File)
				{
					return RemoveDelimiter(this.Key);
				}
				else
				{
					return string.Empty;
				}
			}
		}

		/// <summary>
		/// Gets the full path as a string.
		/// </summary>
		public string FullPath
		{
			get
			{
				switch (this.PathUriType)
				{
					case PathUriTypes.Root:
						return AddDelimiter(this.RootPath); // PATH_DELIMITER;

					case PathUriTypes.Bucket:
						return AddDelimiter(this.BucketName);

					case PathUriTypes.Folder:
						return AddDelimiter($"{this.BucketName}/{this.Key}");

					default:
						return $"{this.BucketName}/{this.Key}";
				}
			}
		}

		public static string RemoveRootPath(string rootPath, string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				return path;
			}
			else if (String.IsNullOrEmpty(rootPath))
			{
				return path;
			}
			else
			{
				if (path.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
				{
					return path.Substring(rootPath.Length);
				}
				else
				{
					return path;
				}
			}
		}

		
		public string RelativePath
		{
			get
			{
				string path = RemoveRootPath(this.RootPath, this.FullPath);

				if (String.IsNullOrEmpty(path))
				{
					return path;
				}
				else
				{
					return Normalize(path).Trim(PATH_DELIMITER);
				}
			} 
		}

		private string Normalize(string path)
		{
			return System.IO.Path.TrimEndingDirectorySeparator(path.Replace(System.IO.Path.DirectorySeparatorChar, PATH_DELIMITER));
		}


		/// <summary>
		/// Add the root folder at the start of the specified path, if a root folder has been set, ensuring that the returned path does
		/// not end with trailing directory separator characters. 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private string UseRootPath(string path)
		{
			path = path.Trim(DirectorySeparators);

			if (String.IsNullOrEmpty(this.RootPath))
			{
				return path;
			}
			else
			{
				if (!String.IsNullOrEmpty(path))
				{
					return $"{this.RootPath}{PATH_DELIMITER}{path}";
				}
				else
				{
					return this.RootPath;
				}
			}
		}

		/// <summary>
		/// Gets the parent path for the path.
		/// </summary>
		/// <remarks>
		/// If the path is the root path, this method returns itself.
		/// </remarks>
		public PathUri Parent
		{
			get
			{
				if (RemoveRootPath(this.RootPath, this.FullPath) == PATH_DELIMITER.ToString())
				{
					// this is the top-level "root", return "self"
					return this;
				}
				else
				{
					string path = RemoveRootPath(this.RootPath, this.FullPath).TrimStart(PATH_DELIMITER);
					int parentPathEnd = path.LastIndexOf(PathUri.PATH_DELIMITER, path.Length - 2);
					return new PathUri(this.RootPath, PathUri.AddDelimiter(path.Substring(0, parentPathEnd + 1)));
				}
			}
		}

		/// <summary>
		/// Return the last part of the path (after the last delimiter), removing any trailing delimiter first.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public string DisplayName
		{
			get
			{
				string normalizedPath = Normalize(System.IO.Path.TrimEndingDirectorySeparator(this.FullPath));
				int position = normalizedPath.LastIndexOf(PATH_DELIMITER);

				if (position < 0)
				{
					return normalizedPath;
				}
				else
				{
					return normalizedPath.Substring(position + 1);
				}
			}
		}

		/// <summary>
		/// Returns an array containing the path segements that make up this path.
		/// </summary>
		public string[] Parts
		{
			get
			{
				return this.FullPath.Split(PathUri.PATH_DELIMITER, StringSplitOptions.RemoveEmptyEntries);
			}
		}

		/// <summary>
		/// Ensure that the specified key ends with the path delimiter.
		/// </summary>
		/// <param name="key">key to check</param>
		/// <returns>
		/// A string which is the specified key with the path delimiter added, if the specified key did not already end with the path delimiter.  In S3,
		/// containers and folders are identified by having the delimiter character as their last character.
		/// </returns>
		public static string AddDelimiter(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				return string.Empty;
			}
			else
			{
				if (!path.EndsWith(PathUri.PATH_DELIMITER))  // && key.Length > 1)
				{
					return path + PathUri.PATH_DELIMITER;
				}
			}

			return path;
		}

		/// <summary>
		/// Remove path delimiter from the end of the specified path, if present.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static string RemoveDelimiter(string path)
		{
			if (path.EndsWith(PATH_DELIMITER))
			{
				return path.TrimEnd(PATH_DELIMITER);
			}
			return path;
		}

		/// <summary>
		/// Returns a string representation of the path.  
		/// </summary>
		/// <remarks>
		/// This value should be used for debugging/logging purposes only.  Use the FullPath or EncodedPath methods to get a string representation of the path.
		/// </remarks>
		/// <returns></returns>
		public override string ToString()
		{
			return this.FullPath;
		}
	}
}
