using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using System.Runtime.CompilerServices;
using System.IO;

namespace Nucleus.Extensions.AmazonS3FileSystemProvider
{
	/// <summary>
	/// Amazon S3 file system provider.
	/// </summary>

	public class FileSystemProvider : Abstractions.FileSystemProviders.FileSystemProvider
	{
		private FileSystemProviderOptions Options { get; } = new();

		/// <summary>
		/// Enum used to specify items to include in the result of a call to ListItems.
		/// </summary>
		[Flags]
		public enum ListItemsOptions
		{
			/// <summary>
			/// Include folders and containers 
			/// </summary>
			Folders = 1,
			/// <summary>
			/// Include files 
			/// </summary>
			Files = 2,
			/// <summary>
			/// Include folders and containers and files.
			/// </summary>
			All = -1
		}

		public FileSystemProvider()
		{

		}

		public override void Configure(IConfigurationSection configSection, string HomeDirectory)
		{
			configSection.Bind(this.Options);
		}

		/// <summary>
		/// Configure and return an instance of the AmazonS3Client class.
		/// </summary>
		/// <returns>
		/// An instance of the AmazonS3Client class which can be used to call S3 API functions.
		/// </returns>
		private AmazonS3Client BuildClient()
		{
			AWSCredentials credentials = new BasicAWSCredentials(this.Options.AccessKey, this.Options.Secret);
			AmazonS3Config config = new AmazonS3Config()
			{
				ServiceURL = this.Options.ServiceUrl
			};

			AmazonS3Client result = new AmazonS3Client(credentials, config);

			return result;
		}

		public override async Task<Folder> CreateFolder(string parentPath, string newFolder)
		{
			PathUri path = new PathUri(parentPath).Combine(newFolder.ToLower(), true);

			using (var client = BuildClient())
			{
				if (path.Parent.PathUriType == PathUri.PathUriTypes.Root)
				{
					throw new NotSupportedException();
				}
				else
				{
					// create a folder					
					PutObjectRequest request = new PutObjectRequest() { BucketName = path.BucketName, Key = PathUri.AddDelimiter(path.Key) };
					PutObjectResponse response = await client.PutObjectAsync(request);
					if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
					{
						throw new AWSCloudProviderException(response);
					}
				}
			}

			return BuildFolder(path);
		}

		public override async Task DeleteFile(string path)
		{
			using (var client = BuildClient())
			{
				PathUri pathUri = new(path);
				DeleteObjectRequest request = new DeleteObjectRequest() { BucketName = pathUri.BucketName, Key = pathUri.Key };
				DeleteObjectResponse response = await client.DeleteObjectAsync(request);
				if (response.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
				{
					throw new AWSCloudProviderException(response);
				}
			}
		}

		public override async Task DeleteFolder(string path, bool recursive)
		{
			using (var client = BuildClient())
			{
				PathUri pathUri = new(PathUri.AddDelimiter(path));

				if (pathUri.PathUriType == PathUri.PathUriTypes.Bucket)
				{
					throw new NotSupportedException();
					// delete a bucket
					////if (!recursive)
					////{
					////	// check that the bucket doesn't contain any items - deleting a bucket will delete everything in the bucket by default
					////	if (ListItems(pathUri, ListItemsOptions.All, 1).Count > 0)
					////	{
					////		throw new AWSCloudProviderException(System.Net.HttpStatusCode.Conflict, "Folder not deleted because it is not empty.");
					////	}
					////}

					////DeleteBucketRequest request = new DeleteBucketRequest() { BucketName = pathUri.BucketName, UseClientRegion = true };
					////DeleteBucketResponse response = client.DeleteBucketAsync(request).Unwrap();
					////if (response.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
					////{
					////	throw new AWSCloudProviderException(response);
					////}
				}
				else if (pathUri.PathUriType == PathUri.PathUriTypes.Folder)
				{
					// delete a folder
					if (!recursive)
					{
						// check that the folder doesn't contain any items - as folders are just a zero length AWS item, deleting one would not
						// check if there are files "in" the folder (because there is no such thing) 
						if ((await ListItems(pathUri, ListItemsOptions.All, 1)).Any())
						{
							throw new AWSCloudProviderException(System.Net.HttpStatusCode.Conflict, $"Folder {path} was not deleted because it is not empty.");
						}
					}
					else
					{
						// recursive: we must delete "child items" ourselves, as S3 does not have a recursive option
						foreach (Abstractions.Models.FileSystem.FileSystemItem child in await ListItems(pathUri, ListItemsOptions.All))
						{
							if (child is Abstractions.Models.FileSystem.Folder)
							{
								await DeleteFolder(child.Path, recursive);
							}
							else if (child is Abstractions.Models.FileSystem.File)
							{
								await DeleteFile(child.Path);
							}
						}
					}

					// delete the "folder"
					DeleteObjectRequest request = new DeleteObjectRequest() { BucketName = pathUri.BucketName, Key = PathUri.RemoveDelimiter(pathUri.Key) };
					DeleteObjectResponse response = await client.DeleteObjectAsync(request);
					if (response.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
					{
						throw new AWSCloudProviderException(response);
					}
				}
			}
		}

		public override async Task<Abstractions.Models.FileSystem.File> GetFile(string path)
		{
			PathUri pathUri = new(path);

			using (var client = BuildClient())
			{
				if (pathUri.PathUriType != PathUri.PathUriTypes.File)
				{
					throw new AWSCloudProviderException(System.Net.HttpStatusCode.BadRequest, "Specified path is not a file.");
				}

				GetObjectMetadataRequest request = new GetObjectMetadataRequest() { BucketName = pathUri.BucketName, Key = pathUri.Key };
				try
				{
					GetObjectMetadataResponse response = await client.GetObjectMetadataAsync(request);
					return await BuildFile(pathUri, response);
				}
				catch (Amazon.S3.AmazonS3Exception e)
				{
					if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
					{
						throw new System.IO.FileNotFoundException();
					}
					throw;
				}

			}
		}

		public override Task<System.Uri> GetFileDirectUrl(string path, DateTime expiresOn)
		{
			// set cache expiry to 30 minutes less than the SAS token expiry so a browser doesn't ever try a direct url that won't work
			long maxAge = (Int64)((expiresOn - DateTime.UtcNow).TotalHours - 0.5) * 60;

			PathUri pathUri = new(path);

			using (var client = BuildClient())
			{
				if (pathUri.PathUriType != PathUri.PathUriTypes.File)
				{
					throw new AWSCloudProviderException(System.Net.HttpStatusCode.BadRequest, "Specified path is not a file.");
				}

				GetPreSignedUrlRequest request = new GetPreSignedUrlRequest() { BucketName = pathUri.BucketName, Key = pathUri.Key, Expires = DateTime.UtcNow.AddMinutes(maxAge) };
				return Task.FromResult(new System.Uri(client.GetPreSignedURL(request)));
			}
		}

		public override async Task<System.IO.Stream> GetFileContents(string path)
		{
			PathUri pathUri = new(path);

			using (var client = BuildClient())
			{
				if (pathUri.PathUriType != PathUri.PathUriTypes.File)
				{
					throw new AWSCloudProviderException(System.Net.HttpStatusCode.BadRequest, "Specified path is not a file.");
				}

				GetObjectRequest request = new GetObjectRequest() { BucketName = pathUri.BucketName, Key = pathUri.Key };
				GetObjectResponse response = await client.GetObjectAsync(request);
				if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
				{
					throw new AWSCloudProviderException(response);
				}

				return response.ResponseStream;
			}
		}

		public override async Task<Folder> GetFolder(string path)
		{
			PathUri pathUri = new(String.IsNullOrEmpty(path) ? path : PathUri.AddDelimiter(path));

			using (var client = BuildClient())
			{
				switch (pathUri.PathUriType)
				{
					case PathUri.PathUriTypes.Root:
						// if we are at the top level, count the number of buckets.  If there is only one, automatically navigate to it

						List<FileSystemItem> buckets = await ListItems(new PathUri(""), ListItemsOptions.Folders);
						if (buckets.Count == 1)
						{
							return buckets.First() as Folder;
						}
						return BuildFolder(new PathUri(""));

					case PathUri.PathUriTypes.Bucket:
						return BuildFolder(pathUri);

					case PathUri.PathUriTypes.Folder:
						// The S3 API methods GetObject and GetObjectMetadata don't work with "folders".  We have to call ListItems on the "parent".

						ListObjectsV2Request request = new ListObjectsV2Request()
						{
							BucketName = pathUri.BucketName,
							Prefix = pathUri.Key,
							Delimiter = PathUri.PATH_DELIMITER,
							MaxKeys = 1,
							FetchOwner = false
						};

						try
						{
							await client.ListObjectsV2Async(request);
							return BuildFolder(pathUri);
						}
						catch (Amazon.S3.AmazonS3Exception e)
						{
							if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
							{
								throw new System.IO.FileNotFoundException();
							}
							throw;
						}

					default:
						throw new AWSCloudProviderException(System.Net.HttpStatusCode.BadRequest, "Specified path is not a folder.");
				}
			}
		}

		public override async Task<Folder> ListFolder(string path)
		{
			return await ListFolder(path, "");
		}

		public override async Task<Folder> ListFolder(string path, string pattern)
		{
			PathUri pathUri = new(PathUri.AddDelimiter(path));

			Folder folder = BuildFolder(pathUri);
			foreach (Abstractions.Models.FileSystem.FileSystemItem item in await ListItems(pathUri, ListItemsOptions.All))
			{
				if (item is Abstractions.Models.FileSystem.Folder)
				{
					folder.Folders.Add(item as Abstractions.Models.FileSystem.Folder);
				}
				else if (item is Abstractions.Models.FileSystem.File)
				{
					folder.Files.Add(item as Abstractions.Models.FileSystem.File);
				}
			}

			return folder;
		}

		public override Task<Abstractions.Models.FileSystem.File> RenameFile(string path, string newName)
		{
			// S3 does not support renames, and the Build[Folder/File]Capabilities functions all signal that renames are not supported, so this
			// function should never be called.
			throw new NotImplementedException();
		}

		public override Task<Folder> RenameFolder(string path, string newName)
		{
			// S3 does not support renames, and the Build[Folder/File]Capabilities functions all signal that renames are not supported, so this
			// function should never be called.
			throw new NotImplementedException();
		}

		public override async Task<Abstractions.Models.FileSystem.File> SaveFile(string parentPath, string newFileName, System.IO.Stream content, bool overwrite)
		{
			PathUri pathUri = new PathUri(parentPath).Combine(newFileName.ToLower(), false);

			using (var client = BuildClient())
			{
				if (pathUri.PathUriType == PathUri.PathUriTypes.Root)
				{
					throw new AWSCloudProviderException(System.Net.HttpStatusCode.Conflict, "Cannot upload file to root folder.");
				}

				PutObjectRequest request = new PutObjectRequest() { BucketName = pathUri.BucketName, Key = pathUri.Key, UseChunkEncoding = false };

				if (content.CanSeek)
				{
					content.Position = 0;
				}

				request.InputStream = content;
				PutObjectResponse response = await client.PutObjectAsync(request);
				if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
				{
					throw new AWSCloudProviderException(response);
				}
			}

			return await BuildFile(pathUri);
		}

		/// <summary>
		/// Returns a value indicating whether the path represents a container (and not a sub-folder or file).
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private Boolean IsContainerPath(string path)
		{
			return GetContainerName(path) == path;
		}

		private string GetContainerName(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				return string.Empty;
			}
			else
			{
				return Normalize(path).Split(new char[] { System.IO.Path.AltDirectorySeparatorChar })[0];
			}
		}


		private string GetFullPath(string containerName, string blobName)
		{
			if (!String.IsNullOrEmpty(containerName))
			{
				return $"{containerName}{System.IO.Path.AltDirectorySeparatorChar}{blobName}";
			}
			else
			{
				return blobName;
			}
		}

		private FileSystemItemCapabilities BuildTopFolderCapabilities()
		{
			return new FileSystemItemCapabilities()
			{
				CanHaveFolders = false,
				CanStoreFiles = false,
				CanRename = false,
				CanDelete = false
			};
		}

		private FileSystemItemCapabilities BuildBucketCapabilities()
		{
			return new FileSystemItemCapabilities()
			{
				CanStoreFiles = true,
				CanRename = false,
				CanDelete = false
			};
		}

		private FileSystemItemCapabilities BuildSubFolderCapabilities()
		{
			return new FileSystemItemCapabilities()
			{
				CanStoreFiles = true,
				CanRename = false,
				CanDelete = true
			};
		}


		private FileSystemItemCapabilities BuildFileCapabilities()
		{
			return new FileSystemItemCapabilities()
			{
				CanStoreFiles = false,
				CanRename = false,
				CanDelete = true,
				CanDirectLink = true
			};
		}

		private async Task<Abstractions.Models.FileSystem.File> BuildFile(S3Object fileInfo)
		{
			PathUri newItem = new PathUri(fileInfo.BucketName, fileInfo.Key);
			Abstractions.Models.FileSystem.File file = await BuildFile(newItem);
			file.Size = fileInfo.Size;
			file.DateModified = fileInfo.LastModified.ToUniversalTime();

			return file;
		}


		private async Task<Abstractions.Models.FileSystem.File> BuildFile(PathUri pathUri)
		{
			return new Abstractions.Models.FileSystem.File()
			{
				Provider = this.Key,
				Path = GetRelativePath(pathUri.FullPath),
				Name = GetDisplayName(pathUri.FullPath),
				Parent = await GetFolder(GetParentPath(pathUri.FullPath)),
				Capabilities = BuildFileCapabilities()
			};
		}

		private async Task<Abstractions.Models.FileSystem.File> BuildFile(PathUri pathUri, GetObjectMetadataResponse response)
		{
			return new Abstractions.Models.FileSystem.File()
			{
				Provider = this.Key,
				Path = GetRelativePath(pathUri.FullPath),
				Name = GetDisplayName(pathUri.FileName),
				DateModified = response.LastModified,
				Parent = await GetFolder(GetParentPath(pathUri.FullPath)),
				Size = response.ContentLength,
				Capabilities = BuildFileCapabilities()
			};
		}

		private Folder BuildFolder(PathUri path)
		{
			if (path.PathUriType == PathUri.PathUriTypes.Root)
			{
				// top level
				return new Folder()
				{
					Provider = this.Key,
					Path = "",
					Name = "/",
					Parent = null,
					Capabilities = BuildTopFolderCapabilities(),
					FolderValidationRules = BuildTopFolderValidationRules()
				};
			}
			else if (path.PathUriType == PathUri.PathUriTypes.Bucket)
			{
				// top level
				return new Folder()
				{
					Provider = this.Key,
					Path = path.BucketName,
					Name = path.BucketName,
					Parent = new Folder() { Provider = this.Key, Path = "" },
					Capabilities = BuildBucketCapabilities(),
					FolderValidationRules = BuildBucketValidationRules()
				};
			}
			else
			{
				return new Folder()
				{
					Provider = this.Key,
					Path = GetRelativePath(path.FullPath),
					Name = GetDisplayName(path.FullPath),
					Parent = new Folder { Provider = this.Key, Path = GetRelativePath(GetParentPath(path.FullPath)) },
					Capabilities = BuildSubFolderCapabilities(),
					FolderValidationRules = BuildSubFolderValidationRules(),
					FileValidationRules = BuildFileValidationRules()
				};
			}
		}

		private FileSystemValidationRule[] BuildTopFolderValidationRules()
		{
			return new FileSystemValidationRule[]
			{
				new FileSystemValidationRule()
				{
					ValidationExpression = "^[0-9a-z]{1}[0-9a-z-]{2,61}[0-9a-z]$" , ErrorMessage = "S3 top level folders (containers) must start and end with a letter or number, contain only letters, numbers and dashes, and must be lower case.",
				},
				new FileSystemValidationRule()
				{
					ValidationExpression = "^(?!.*--)" , ErrorMessage = "S3 top level folders (containers) must not contain consecutive dashes.",
				}
			};
		}

		private FileSystemValidationRule[] BuildFileValidationRules()
		{
			return new FileSystemValidationRule[]
				{
					new FileSystemValidationRule()
					{
						ValidationExpression = "^.{1,1024}$" , ErrorMessage = "S3 file names must be 1-1024 characters long.",
					},
					new FileSystemValidationRule()
					{
						ValidationExpression = "^[^\\/\\\\]+$" , ErrorMessage = "S3 file names cannot contain the '/' or '\\' character.",
					}
				};
		}

		private FileSystemValidationRule[] BuildBucketValidationRules()
		{
			return BuildSubFolderValidationRules();
		}

		private FileSystemValidationRule[] BuildSubFolderValidationRules()
		{
			return new FileSystemValidationRule[]
				{
					new FileSystemValidationRule()
					{
						ValidationExpression = "^.{1,1024}$" , ErrorMessage = "S3 sub-folders must be 1-1024 characters long.",
					},
					new FileSystemValidationRule()
					{
						ValidationExpression = "^[^\\/\\\\]+$" , ErrorMessage = "S3 sub-folders cannot contain the '/' or '\\' character.",
					}
				};
		}

		/// <summary>
		/// Return a list of items in the specified path.
		/// </summary>
		/// <param name="Path">Path to read items within</param>
		/// <param name="Options">Specifies whether to include folders, files or both</param>
		/// <returns>
		/// A list of items in the specified path.
		/// </returns>
		public async Task<List<Abstractions.Models.FileSystem.FileSystemItem>> ListItems(PathUri Path, ListItemsOptions Options)
		{
			return await ListItems(Path, Options, -1);
		}

		/// <summary>
		/// Return a list of items within the specified path.
		/// </summary>
		/// <param name="path">Path to read items within</param>
		/// <param name="Options">Specifies whether to include folders, files or both</param>
		/// <param name="Limit">Specifies the maximum number of items to return</param>
		/// <returns>
		/// A list of items in the specified path.
		/// </returns>
		public async Task<List<Abstractions.Models.FileSystem.FileSystemItem>> ListItems(PathUri path, ListItemsOptions options, int limit)
		{
			List<Abstractions.Models.FileSystem.FileSystemItem> results = new();
			ListObjectsV2Response response;
			string startAfter = "";
			Boolean readNextBlock = true;
			int itemsAdded;

			using (var client = BuildClient())
			{
				if (path.PathUriType == PathUri.PathUriTypes.Root)
				{
					// ignore Options (ListItemsOptions) for the top level, as there can only ever be buckets at this level	
					// We can't use Limit in the API call, because there's no such parameter
					foreach (S3Bucket item in (await client.ListBucketsAsync()).Buckets)
					{
						results.Add(BuildFolder(new PathUri(PathUri.AddDelimiter(item.BucketName))));
						if (results.Count == limit)
						{
							break;
						}
					}
				}
				else
				{
					itemsAdded = 0;
					while (readNextBlock)
					{
						// S3 returns the requested folder as well as its items, so we need to add 1 to Limit, otherwise when we exclude the "self" item, 
						// we would end up with no items in the result.
						ListObjectsV2Request request = new ListObjectsV2Request()
						{
							BucketName = path.BucketName,
							Prefix = path.Key,
							Delimiter = PathUri.PATH_DELIMITER,
							StartAfter = startAfter,
							MaxKeys = (limit == -1 ? 1000 : limit + 1),
							FetchOwner = false
						};

						response = await client.ListObjectsV2Async(request);

						// https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/S3/TS3Object.html
						if (options.HasFlag(ListItemsOptions.Folders))
						{
							// Folders which contain files are returned in the .CommonPrefixes string array
							foreach (string item in response.CommonPrefixes)
							{
								// Remove the current path from the start of the returned Key, so that we just have the folder name
								string folderName = !String.IsNullOrEmpty(path.Key) ? item.Replace($"{path.Key}", "") : item;
								results.Add(BuildFolder(new PathUri(path.BucketName).Combine(item, true)));
								itemsAdded += 1;

								if (results.Count == limit)
								{
									break;
								}
							}
						}

						foreach (S3Object item in response.S3Objects)
						{
							PathUri childFolderUri = new PathUri(path.BucketName).Combine(item.Key, true);

							if (childFolderUri.PathUriType != PathUri.PathUriTypes.File && item.Size == 0)
							{
								if (options.HasFlag(ListItemsOptions.Folders))
								{
									// S3 returns the requested "folder" itself (as well as its children) so we have to make sure that the folder being
									// added isn't the parent folder.
									if (childFolderUri.FullPath != path.FullPath)
									{
										results.Add(BuildFolder(childFolderUri));
									}
									itemsAdded += 1;
								}
							}
							else
							{
								if (options.HasFlag(ListItemsOptions.Files))
								{
									//PathUri newItem = new PathUri(path.BucketName, item.Key);
									Abstractions.Models.FileSystem.File file = await BuildFile(item);
									//file.Size = item.Size;
									results.Add(file);
									itemsAdded += 1;
								}
							}

							if (results.Count == limit)
							{
								break;
							}
						}

						if (response.IsTruncated && results.Count < limit && itemsAdded > 0)
						{
							// Call ListObjectsV2 API again to get the next lot of files/folders (ListObjectsV2 only reads 1000 at a time)
							readNextBlock = true;
							startAfter = response.S3Objects[response.S3Objects.Count - 1].Key;
						}
						else
						{
							readNextBlock = false;
						}
					}
				}
			}

			return results;
		}

		/// <summary>
		/// Return the last part of the path (after the last delimiter), removing any trailing delimiter first.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private string GetDisplayName(string path)
		{
			string normalizedPath = Normalize(System.IO.Path.TrimEndingDirectorySeparator(path));
			int position = normalizedPath.LastIndexOf(System.IO.Path.AltDirectorySeparatorChar);

			if (position < 0)
			{
				return normalizedPath;
			}
			else
			{
				return normalizedPath.Substring(position + 1);
			}
		}

		/// <summary>
		/// Return the "parent" path, that is, everything before the last delimiter.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private string GetParentPath(string path)
		{
			int position = Normalize(path).LastIndexOf(System.IO.Path.AltDirectorySeparatorChar);

			if (position < 0)
			{
				return "";
			}
			else
			{
				return path.Substring(0, position);
			}
		}

		private string GetRelativePath(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				return path;
			}
			else
			{
				string relativePath = Normalize(path);
				if (relativePath.StartsWith(System.IO.Path.AltDirectorySeparatorChar))
				{
					if (relativePath.Length > 1)
					{
						// remove leading/trailing "/"
						return System.IO.Path.TrimEndingDirectorySeparator(relativePath[1..]);
					}
					else
					{
						// if the path "/" was passed in, return an empty string (for the LocalFileSystemProvider, an empty string is the "root" path)
						return "";
					}
				}
				else
				{
					return System.IO.Path.TrimEndingDirectorySeparator(relativePath);
				}
			}
		}

		private string Normalize(string path)
		{
			return System.IO.Path.TrimEndingDirectorySeparator(path.Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
		}
	}
}
