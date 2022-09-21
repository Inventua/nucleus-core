using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

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
		public const string PATH_DELIMITER = "/";

		private const string BUCKET_VALIDATION_REGEX = "^[A-Za-z0-9]{1}[A-Za-z\\-_0-9]{3,62}$";
		private const string FOLDER_VALIDATION_REGEX = "^[^\\/]*$";
		private const string FILE_VALIDATION_REGEX   = "^[^\\/]*$";

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
		public PathUri(string bucketName, string key)
		{
			this.BucketName = bucketName;
			this.Key = key;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="Path">String representation of the path</param>
		public PathUri(string path)
		{
			Parse(path ?? "");
		}

		private void Parse(string Path)
		{
			int keyPosition;
			string[] pathParts = Path.Split(PATH_DELIMITER, StringSplitOptions.RemoveEmptyEntries);

			if (pathParts.Length > 0)
			{
				this.BucketName = pathParts[0];

				keyPosition = Path.IndexOf(this.BucketName) + this.BucketName.Length + 1;
				if (Path.Length > keyPosition)
				{
					this.Key = Path.Substring(keyPosition);
				}
				else
				{
					this.Key = "";
				}
			}
			else
			{
				this.BucketName = PATH_DELIMITER;
			}
		}

		/// <summary>
		/// Gets the path type.
		/// </summary>
		public PathUriTypes PathUriType
		{
			get
			{
				if (String.IsNullOrEmpty(this.Key) && (String.IsNullOrEmpty(this.BucketName) || this.BucketName == PATH_DELIMITER))
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
						return PATH_DELIMITER;

					case PathUriTypes.Bucket:
						return $"{PathUri.AddDelimiter(this.BucketName)}";

					default:
						return $"{this.BucketName}/{this.Key}";
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
				if (this.FullPath == PATH_DELIMITER)
				{
					// this is the top-level "root", return "self"
					return this;
				}
				else
				{
					int parentPathEnd = this.FullPath.LastIndexOf(PathUri.PATH_DELIMITER, this.FullPath.Length - 2);
					return new PathUri(PathUri.AddDelimiter(this.FullPath.Substring(0, parentPathEnd + 1)));
				}
			}
		}

		/// <summary>
		/// Combine the specified relative path with this path and returns a new PathUri instance.
		/// </summary>
		/// <param name="parentPath">Folder path</param>
		/// <param name="isFolder">Specifies whether to append PATH_DEMIMITER to the end of the path to indicate that the Uri represents a folder.</param>
		/// <returns></returns>
		public PathUri Combine(string parentPath, Boolean isFolder)
		{
			if (isFolder)
			{
				parentPath = AddDelimiter(parentPath);
			}
			return new PathUri(AddDelimiter(this.FullPath) + parentPath);
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
		/// Check that a bucket name is valid.
		/// </summary>
		/// <param name="value">Bucket name</param>
		/// <remarks>
		/// Top level folders can contain only lowercase letters, numbers, dashes (-), underscores (_), 
		/// must start and end with a number or letter, and be between 3-63 characters.
		/// </remarks>
		/// <returns>True if valid, false if not.</returns>
		public static Boolean IsValidBucketName(string value)
		{
			return System.Text.RegularExpressions.Regex.IsMatch(value, BUCKET_VALIDATION_REGEX);
		}

		/// <summary>
		/// Check that a folder name is valid.
		/// </summary>
		/// <param name="value">Folder name</param>
		/// <remarks>
		/// Folders can contain any characters except the path delimiter.
		/// </remarks>
		/// <returns>True if valid, false if not.</returns>
		public static Boolean IsValidFolderName(string value)
		{
			return System.Text.RegularExpressions.Regex.IsMatch(value, FOLDER_VALIDATION_REGEX);
		}

		/// <summary>
		/// Check that a file name is valid.
		/// </summary>
		/// <param name="value">File name</param>
		/// <remarks>
		/// Files can contain any characters except the path delimiter.
		/// </remarks>
		/// <returns>True if valid, false if not.</returns>
		public static Boolean IsValidFileName(string value)
		{
			return System.Text.RegularExpressions.Regex.IsMatch(value, FILE_VALIDATION_REGEX);
		}

		/// <summary>
		/// Ensure that the specified key ends with the path delimiter.
		/// </summary>
		/// <param name="key">key to check</param>
		/// <returns>
		/// A string which is the specified key with the path delimiter added, if the specified key did not already end with the path delimiter.  In S3,
		/// containers and folders are identified by having the delimiter character as their last character.
		/// </returns>
		public static string AddDelimiter(string key)
		{
			if (String.IsNullOrEmpty(key))
			{
				return string.Empty;
			}
			else
			{
				if (!key.EndsWith(PathUri.PATH_DELIMITER))  // && key.Length > 1)
				{
					return key + PathUri.PATH_DELIMITER;
				}
			}

			return key;
		}

		/// <summary>
		/// Remove the trailing delimiter character from the specified key, if it is present.  This function is used for some S3 API calls which 
		/// require that the specified key does not include the trailing delimiter character.
		/// </summary>
		/// <param name="key">key to check</param>
		/// <returns>
		/// A string which is the specified key with the trailing delimiter character removed, if present. 
		/// </returns>
		public static string RemoveDelimiter(string key)
		{
			if (String.IsNullOrEmpty(key))
			{
				return string.Empty;
			}
			else
			{
				if (key.EndsWith(PathUri.PATH_DELIMITER))
				{
					if (key.Length > 1)
					{
						return key.Substring(0, key.Length - 1);
					}
					else
					{
						return string.Empty;
					}
				}
			}

			return key;
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
