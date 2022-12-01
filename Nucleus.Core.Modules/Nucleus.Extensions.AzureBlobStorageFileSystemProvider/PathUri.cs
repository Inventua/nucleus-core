using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Nucleus.Extensions.AzureBlobStorageFileSystemProvider
{
	/// <summary>
	/// Represents a Azure Storage object path and provides parsing and access to parts of the path.
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
			/// The path is the root, which contains containers.
			/// </summary>
			Root,
			/// <summary>
			/// The path is a container.
			/// </summary>
			Container,
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
		public string ContainerName { get; private set; }

		/// <value>
		/// Key component of the path.
		/// </value>
		public string Key { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PathUri()
		{
			this.ContainerName = string.Empty;
			this.Key = string.Empty;
		}

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="rootPath"></param>
    /// <param name="containerName">Bucket (container) name</param>
    /// <param name="Key">Item key</param>
    public PathUri(string rootPath, string containerName, string key)
		{
			this.RootPath = RemoveDelimiter(rootPath);
			this.ContainerName = containerName;
			this.Key = key;
		}

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="rootPath"></param>
    /// <param name="Path">String representation of the path</param>
    public PathUri(string rootPath, string path) : this(rootPath, path, false) {	}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="Path">String representation of the path</param>
		public PathUri(string rootPath, string path, Boolean isFolder)
		{
			this.RootPath = RemoveDelimiter(rootPath);
			Parse(isFolder && !String.IsNullOrEmpty(path) ? AddDelimiter(path) : path ?? "");
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
				this.ContainerName = pathParts[0];

				keyPosition = fullPath.IndexOf(this.ContainerName) + this.ContainerName.Length + 1;
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
				this.ContainerName = PATH_DELIMITER.ToString();
			}
		}

		/// <summary>
		/// Gets the path type.
		/// </summary>
		public PathUriTypes PathUriType
		{
			get
			{
				if (String.IsNullOrEmpty(this.Key) && (String.IsNullOrEmpty(this.ContainerName) || this.ContainerName == PATH_DELIMITER.ToString()))
				{
					return PathUriTypes.Root;
				}
				else if (!String.IsNullOrEmpty(this.ContainerName) && string.IsNullOrEmpty(this.Key))
				{
					return PathUriTypes.Container;
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
					return RemoveDelimiter(this.Parts.Last());
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

					case PathUriTypes.Container:
						return AddDelimiter(this.ContainerName);

					case PathUriTypes.Folder:
						return AddDelimiter($"{this.ContainerName}/{this.Key}");

					default:
						return $"{this.ContainerName}/{this.Key}";
				}
			}
		}

		private string RemoveRootPath(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				return path;
			}
			else if (String.IsNullOrEmpty(this.RootPath))
			{
				return path;
			}
			else
			{
				if (path.StartsWith(this.RootPath, StringComparison.OrdinalIgnoreCase))
				{
					return path.Substring(this.RootPath.Length);
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
				string path = RemoveRootPath(this.FullPath);

				if (String.IsNullOrEmpty(path))
				{
					return path;
				}
				else
				{
					return Normalize(path).Trim(PATH_DELIMITER);
					////if (relativePath.StartsWith(PATH_DELIMITER))
					////{
					////	if (relativePath.Length > 1)
					////	{
					////		// remove leading/trailing "/"
					////		return (relativePath[1..]).Trim(PATH_DELIMITER);
					////	}
					////	else
					////	{
					////		// if the path "/" was passed in, return an empty string (an empty string is the "root" path)
					////		return "";
					////	}
					////}
					////else
					////{
					////	return System.IO.Path.TrimEndingDirectorySeparator(relativePath);
					////}
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
				if (RemoveRootPath(this.FullPath) == PATH_DELIMITER.ToString())
				{
					// this is the top-level "root", return "self"
					return this;
				}
				else
				{
					string path = RemoveRootPath(this.FullPath).TrimStart(PATH_DELIMITER);
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

		///// <summary>
		///// Return the "parent" path, that is, everything before the last delimiter.
		///// </summary>
		///// <param name="path"></param>
		///// <returns></returns>
		//public PathUri GetParentPath
		//{
		//	get
		//	{
		//		int position = Normalize(this.FullPath).LastIndexOf(PATH_DELIMITER);

		//		if (position < 0)
		//		{
		//			return new(this.RootPath, "");
		//		}
		//		else
		//		{
		//			return new(this.RootPath, this.FullPath.Substring(0, position));
		//		}
		//	}
		//}


		///// <summary>
		///// Combine the specified relative path with this path and returns a new PathUri instance.
		///// </summary>
		///// <param name="parentPath">Folder path</param>
		///// <param name="isFolder">Specifies whether to append PATH_DEMIMITER to the end of the path to indicate that the Uri represents a folder.</param>
		///// <returns></returns>
		//public PathUri Combine(string parentPath, Boolean isFolder)
		//{
		//	if (isFolder)
		//	{
		//		parentPath = AddDelimiter(parentPath);
		//	}
		//	return new PathUri(this.RootPath, AddDelimiter(this.FullPath) + parentPath);
		//}

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
