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

namespace Nucleus.Extensions.AmazonS3FileSystemProvider
{
	/// <summary>
	/// Amazon S3 file system provider.
	/// </summary>

	public class FileSystemProvider : Abstractions.FileSystemProviders.FileSystemProvider
	{
		private FileSystemProviderOptions Options { get; } = new();
		private string RootPath { get; set; }

		
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
			if (!String.IsNullOrEmpty(this.Options.RootPath))
			{
				this.RootPath = this.Options.RootPath;
			}
			else
			{
				this.RootPath = "";
			}
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
			PathUri path = new PathUri(this.RootPath, parentPath, newFolder, true);

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
				PathUri pathUri = new(this.RootPath, path);
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
				PathUri pathUri = new(this.RootPath, path, true);

				if (pathUri.PathUriType == PathUri.PathUriTypes.Bucket)
				{
					throw new NotSupportedException();
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
					DeleteObjectRequest request = new DeleteObjectRequest() { BucketName = pathUri.BucketName, Key = pathUri.Key };
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
			PathUri pathUri = new(this.RootPath, path);

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
					return BuildFile(pathUri, response);
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

			PathUri pathUri = new(this.RootPath, path);

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
			PathUri pathUri = new(this.RootPath, path);

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
			PathUri pathUri = new(this.RootPath, path, true);

			using (var client = BuildClient())
			{
				switch (pathUri.PathUriType)
				{
					case PathUri.PathUriTypes.Root:
						// if we are at the top level, count the number of buckets.  If there is only one, automatically navigate to it

						List<FileSystemItem> buckets = await ListItems(new PathUri(this.RootPath, "", true), ListItemsOptions.Folders);
						if (buckets.Count == 1)
						{
							return buckets.First() as Folder;
						}
						return BuildFolder(new PathUri(this.RootPath, "", true));

					case PathUri.PathUriTypes.Bucket:
						return BuildFolder(pathUri);

					case PathUri.PathUriTypes.Folder:
						// The S3 API methods GetObject and GetObjectMetadata don't work with "folders".  We have to call ListItems on the "parent".

						ListObjectsV2Request request = new ListObjectsV2Request()
						{
							BucketName = pathUri.BucketName,
							Prefix = pathUri.Key,
							Delimiter = PathUri.PATH_DELIMITER.ToString(),
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
			PathUri pathUri = new(this.RootPath, path, true);

			Folder folder = BuildFolder(pathUri);
			foreach (Abstractions.Models.FileSystem.FileSystemItem item in await ListItems(pathUri, ListItemsOptions.All))
			{
				if (item is Abstractions.Models.FileSystem.Folder)
				{
					folder.Folders.Add(item as Abstractions.Models.FileSystem.Folder);
				}
				else if (item is Abstractions.Models.FileSystem.File)
				{
          if (String.IsNullOrEmpty(pattern) || System.Text.RegularExpressions.Regex.IsMatch(item.Name, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
          {
            folder.Files.Add(item as Abstractions.Models.FileSystem.File);
          }
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
			PathUri pathUri = new PathUri(this.RootPath, parentPath, newFileName, false);

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

			return BuildFile(pathUri);
		}

		private Abstractions.Models.FileSystem.File BuildFile(S3Object fileInfo)
		{
			PathUri newItem = new PathUri(this.RootPath, PathUri.RemoveRootPath(this.RootPath, fileInfo.BucketName), fileInfo.Key, false);
			Abstractions.Models.FileSystem.File file = BuildFile(newItem);
			file.Size = fileInfo.Size;
			file.DateModified = fileInfo.LastModified.ToUniversalTime();

			return file;
		}


		private Abstractions.Models.FileSystem.File BuildFile(PathUri pathUri)
		{
			return new Abstractions.Models.FileSystem.File()
			{
				Provider = this.Key,
				Path = pathUri.RelativePath,
				Name = pathUri.DisplayName,
				Parent = new Folder { Provider = this.Key, Path = pathUri.Parent.RelativePath },
				Capabilities = FileSystemCapabilities.File
			};
		}

		private Abstractions.Models.FileSystem.File BuildFile(PathUri pathUri, GetObjectMetadataResponse response)
		{
			Abstractions.Models.FileSystem.File file = BuildFile(pathUri);
			file.DateModified = response.LastModified;
			file.Size = response.ContentLength;

			////return new Abstractions.Models.FileSystem.File()
			////{
			////	Provider = this.Key,
			////	Path = pathUri.RelativePath,
			////	Name = pathUri.DisplayName,
			////	DateModified = response.LastModified,
			////	Parent = new Folder { Provider = this.Key, Path = pathUri.Parent.RelativePath },
			////	Size = response.ContentLength,
			////	Capabilities = FileSystemCapabilities.File
			////};

			return file;
		}

		private Folder BuildFolder(PathUri path)
		{
			if (path.PathUriType == PathUri.PathUriTypes.Root)
			{
				// root level
				return new Folder()
				{
					Provider = this.Key,
					Path = "",
					Name = "/",
					Parent = null,
					Capabilities = FileSystemCapabilities.Root,
					FolderValidationRules = FileSystemValidationRules.Root
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
					Parent = new Folder { Provider = this.Key, Path = path.Parent.RelativePath },
          Capabilities = FileSystemCapabilities.Bucket,
					FolderValidationRules = FileSystemValidationRules.Bucket
				};
			}
			else
			{
				return new Folder()
				{
					Provider = this.Key,
					Path = path.RelativePath,
					Name = path.DisplayName,
					Parent = new Folder { Provider = this.Key, Path = path.Parent.RelativePath },
					Capabilities = FileSystemCapabilities.Folder,
					FolderValidationRules = FileSystemValidationRules.Folder,
					FileValidationRules = FileSystemValidationRules.File
				};
			}
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
						results.Add(BuildFolder(new PathUri(this.RootPath, PathUri.RemoveRootPath(this.RootPath, item.BucketName), "", true)));
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
							Delimiter = PathUri.PATH_DELIMITER.ToString(),
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
								results.Add(BuildFolder(new PathUri(this.RootPath, PathUri.RemoveRootPath(this.RootPath, path.BucketName), item, true))); 
								itemsAdded += 1;

								if (results.Count == limit)
								{
									break;
								}
							}
						}

						foreach (S3Object item in response.S3Objects)
						{
							PathUri childFolderUri = new PathUri(this.RootPath, PathUri.RemoveRootPath(this.RootPath, path.BucketName), item.Key, true);

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
									Abstractions.Models.FileSystem.File file = BuildFile(item);
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
	}
}
