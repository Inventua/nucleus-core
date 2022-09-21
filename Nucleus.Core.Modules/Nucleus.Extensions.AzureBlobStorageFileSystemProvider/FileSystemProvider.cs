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
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure;

namespace Nucleus.Extensions.AzureBlobStorageFileSystemProvider
{
	/// <summary>
	/// Azure Blob Storage file system provider.
	/// </summary>

	// Naming and Referencing Containers, Blobs, and Metadata:
	// https://docs.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata
	public class FileSystemProvider : Abstractions.FileSystemProviders.FileSystemProvider
	{
		private FileSystemProviderOptions Options { get; } = new();
		private string RootPath { get; set; }

		private static char[] DirectorySeparators = new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar };

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

		public override async Task<Folder> CreateFolder(string parentPath, string newFolder)
		{
			string fullPath = JoinPath(UseRootFolder(parentPath), newFolder);
			BlobServiceClient client = new(this.Options.ConnectionString);

			if (IsContainerPath(fullPath))
			{
				if (!ValidateContainerName(GetContainerName(fullPath)))
				{
					throw new InvalidOperationException($"Cannot create folder '{newFolder}' because it contains spaces or other invalid characters.  Top level folders (containers) in Azure Storage must be lower case, and contain only letters, numbers and dashes.");
				}
				// folder being created is at the top level, create a container
				await client.CreateBlobContainerAsync(GetContainerName(fullPath));
			}
			else
			{
				// folder is "in" a container, create zero-length blob to represent a folder 
				BlobContainerClient containerClient = client.GetBlobContainerClient(GetContainerName(fullPath));
				BlobClient blobClient = containerClient.GetBlobClient(GetBlobName(fullPath));
				await blobClient.UploadAsync(new System.IO.MemoryStream());
			}

			return BuildFolder(fullPath);
		}

		public override async Task DeleteFile(string path)
		{
			path = UseRootFolder(path);
			BlobServiceClient client = new(this.Options.ConnectionString);
			BlobContainerClient containerClient = client.GetBlobContainerClient(GetContainerName(path));
			await containerClient.DeleteBlobAsync(GetBlobName(path));
		}

		public override async Task DeleteFolder(string path, bool recursive)
		{
			path = UseRootFolder(path);
			Folder existingFolder = await ListFolder(path);

			if (IsContainerPath(path))
			{
				// folder is at top level (is a container)
				if (existingFolder.Folders.Any() || existingFolder.Files.Any())
				{
					if (!recursive)
					{
						throw new InvalidOperationException("Folder is not empty.");
					}

					// We don't have to delete the blobs inside the container one by one, Azure storage will remove the whole container and its contents
					// when it is deleted.
				}

				BlobServiceClient client = new(this.Options.ConnectionString);
				BlobContainerClient containerClient = client.GetBlobContainerClient(GetContainerName(path));
				containerClient.Delete();
			}
			else
			{
				// folder is not a the top level (is a folder represented by a zero-length file).  Check that the "folder" is empty and delete it
				if (existingFolder.Folders.Any() || existingFolder.Files.Any())
				{
					if (!recursive)
					{
						throw new InvalidOperationException("Folder is not empty.");
					}
					else
					{
						foreach (File file in existingFolder.Files)
						{
							await DeleteFile(file.Path);
						}

						foreach (Folder folder in existingFolder.Folders)
						{
							await DeleteFolder(folder.Path, recursive);
						}
					}
				}

				BlobServiceClient client = new(this.Options.ConnectionString);
				BlobContainerClient containerClient = client.GetBlobContainerClient(GetContainerName(path));
				containerClient.DeleteBlob(GetBlobName(path));
			}
		}

		public override async Task<Abstractions.Models.FileSystem.File> GetFile(string path)
		{
			path = UseRootFolder(path);

			try
			{
				BlobClient blobClient = new(this.Options.ConnectionString, GetContainerName(path), GetBlobName(path));
				Azure.Response<BlobProperties> response = await blobClient.GetPropertiesAsync();
				return await BuildFile(path, response.Value);
			}
			catch (Azure.RequestFailedException ex)
			{
				if (IsNotFoundException(ex))
				{
					throw new System.IO.FileNotFoundException();
				}
				else
				{
					throw;
				}
			}
		}

		public override Task<System.Uri> GetFileDirectUrl(string path, DateTime expiresOn)
		{
			path = UseRootFolder(path);
			// set cache expiry to 30 minutes less than the SAS token expiry so a browser doesn't ever try a direct url that won't work
			long maxAge = (Int64)((expiresOn - DateTime.UtcNow).TotalHours - 0.5) * 60;
			BlobClient blobClient = new(this.Options.ConnectionString, GetContainerName(path), GetBlobName(path));
			
			if (blobClient.CanGenerateSasUri)
			{
				try
				{
					Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider extensionProvider = new();

					if (!extensionProvider.TryGetContentType(path, out string mimeType))
					{
						mimeType = "application/octet-stream";
					}
					else
					{
						if (mimeType.StartsWith("text/") && !mimeType.Contains("utf-8", StringComparison.OrdinalIgnoreCase))
						{
							mimeType += "; charset=utf-8";
						}
					}

					Azure.Storage.Sas.BlobSasBuilder options = new(Azure.Storage.Sas.BlobSasPermissions.Read, expiresOn)
					{ 
						CacheControl = $"public, max-age={maxAge}",
						ContentType = mimeType
					};

					return Task.FromResult(blobClient.GenerateSasUri(options));
				}
				catch (Azure.RequestFailedException ex)
				{
					if (IsNotFoundException(ex))
					{
						throw new System.IO.FileNotFoundException();
					}
					else
					{
						throw;
					}
				}
			}

			return null;
		}

		public override async Task<System.IO.Stream> GetFileContents(string path)
		{
			path = UseRootFolder(path);

			try
			{
				BlobClient blobClient = new(this.Options.ConnectionString, GetContainerName(path), GetBlobName(path));
				return await blobClient.OpenReadAsync();
			}
			catch (Azure.RequestFailedException ex)
			{
				if (IsNotFoundException(ex))
				{
					throw new System.IO.FileNotFoundException();
				}
				else
				{
					throw;
				}
			}
		}

		public override async Task<Folder> GetFolder(string path)
		{
			BlobServiceClient client = new(this.Options.ConnectionString);
			path = UseRootFolder(path);

			if (String.IsNullOrEmpty(path))
			{
				return BuildFolder("");  // represents the root folder
			}
			else
			{
				if (IsContainerPath(path))
				{
					// Path is a top level container
					BlobContainerClient containerClient = client.GetBlobContainerClient(GetContainerName(path));

					try
					{
						Azure.Response<BlobContainerProperties> response = await containerClient.GetPropertiesAsync();
						return BuildFolder(path, response.Value);
					}
					catch (Azure.RequestFailedException ex)
					{
						if (IsNotFoundException(ex))
						{
							throw new System.IO.FileNotFoundException();
						}
						else
						{
							throw;
						}
					}
				}
				else
				{
					// path is a "folder", in a container.  Folders are represented as zero-length blobs
					BlobClient blobClient = new(this.Options.ConnectionString, GetContainerName(path), GetBlobName(path));

					try
					{
						Azure.Response<BlobProperties> response = await blobClient.GetPropertiesAsync();
						return BuildFolder(path, response.Value);
					}
					catch (Azure.RequestFailedException ex)
					{
						if (IsNotFoundException(ex))
						{
							throw new System.IO.FileNotFoundException();
						}
						else
						{
							throw;
						}
					}
				}
			}

		}

		public override async Task<Folder> ListFolder(string path)
		{
			return await ListFolder(path, "");
		}

		public override async Task<Folder> ListFolder(string path, string pattern)
		{
			BlobServiceClient client = new(this.Options.ConnectionString);
			path = UseRootFolder(path);
			BlobContainerClient containerClient = client.GetBlobContainerClient(GetContainerName(path));
			Folder folder = BuildFolder(path);

			// In Azure blob storage, containers can only exist at the top level
			if (String.IsNullOrEmpty(path))
			{
				await foreach (BlobContainerItem item in client.GetBlobContainersAsync(BlobContainerTraits.None, BlobContainerStates.None, GetBlobName(path)))
				{
					folder.Folders.Add(BuildFolder(item));
				}
			}

			// In Azure blob storage, top level cannot contain files (only containers)
			if (!String.IsNullOrEmpty(path))
			{
				string prefix = GetBlobName(path);
				if (!String.IsNullOrEmpty(prefix))
				{
					// if the path is not at the top level of a container (not IsNullOrEmpty) then it has to end with the delimiter, per https://docs.microsoft.com/en-us/dotnet/api/azure.storage.blobs.blobcontainerclient.getblobsbyhierarchy?view=azure-dotnet
					// "The value of a prefix is substring+delimiter"
					prefix += System.IO.Path.AltDirectorySeparatorChar;
				}

				try
				{
					await foreach (BlobHierarchyItem item in containerClient.GetBlobsByHierarchyAsync(Azure.Storage.Blobs.Models.BlobTraits.None, BlobStates.None, new string(System.IO.Path.AltDirectorySeparatorChar, 1), prefix))
					{
						//GetFullPath(containerClient.Name, item.Prefix)
						if (!item.IsBlob)
						{
							// Ensure that we are not adding the folder to itself
							if (JoinPath(containerClient.Name, item.Prefix) != path)
							{
								folder.Folders.Add(BuildFolder(containerClient.Name + System.IO.Path.AltDirectorySeparatorChar + item.Prefix));
							}
						}
						else
						{
							if (item.Blob.Properties.ContentLength == 0)
							{
								// item is a zero-length blob, used to represent a folder.  The zero-length blobs are required in order to allow for empty "folders".  

								// Ensure that we are not adding a sub-folder to itself
								if (JoinPath(containerClient.Name, item.Blob.Name) != path)
								{
									Folder subfolder = BuildFolder(containerClient.Name, item.Blob);
									// When a folder contains files, .GetBlobsByHierarchy will have already returned it as an item with .IsBlob set to false, so we 
									// have to check that the folder hasn't already been added to the result before adding it.
									if (!folder.Folders.Where(folder => folder.Path == subfolder.Path).Any())
									{
										folder.Folders.Add(subfolder);
									}
								}
							}
							else
							{
								// Item is a file

								// Add file if it matches the specifed pattern
								if (String.IsNullOrEmpty(pattern) || System.Text.RegularExpressions.Regex.IsMatch(item?.Blob.Name, pattern))
								{
									folder.Files.Add(await BuildFile(containerClient.Name, item.Blob));
								}
							}
						}
					}
				}
				catch (Azure.RequestFailedException ex)
				{
					if (IsNotFoundException(ex))
					{
						throw new System.IO.FileNotFoundException();
					}
					else
					{
						throw;
					}
				}
			}

			return folder;
		}

		public override Task<Abstractions.Models.FileSystem.File> RenameFile(string path, string newName)
		{
			throw new NotImplementedException();
		}

		public override Task<Folder> RenameFolder(string path, string newName)
		{
			throw new NotImplementedException();
		}

		public override async Task<Abstractions.Models.FileSystem.File> SaveFile(string parentPath, string newFileName, System.IO.Stream content, bool overwrite)
		{
			parentPath = UseRootFolder(parentPath);
			string newObjectPath = $"{parentPath}{System.IO.Path.AltDirectorySeparatorChar}{newFileName}";
			BlobServiceClient client = new(this.Options.ConnectionString);
			BlobContainerClient containerClient = client.GetBlobContainerClient(GetContainerName(parentPath));
			BlobClient blobClient = containerClient.GetBlobClient(GetBlobName(newObjectPath));

			Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider extensionProvider = new();

			if (!extensionProvider.TryGetContentType(newFileName, out string mimeType))
			{
				mimeType = "application/octet-stream";
			}

			await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = mimeType, CacheControl = "max-age=3600" });

			return await BuildFile(newObjectPath);
		}

		/// <summary>
		/// Returns a value indicating whether the path represents a container (and not a sub-folder or file).
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private Boolean IsContainerPath(string path)
		{
			return GetContainerName(path).Equals( path, StringComparison.OrdinalIgnoreCase);
		}

		private string GetContainerName(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				return string.Empty;
			}
			else
			{
				return path.Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
					.Split(new char[] { System.IO.Path.AltDirectorySeparatorChar })[0]
					.ToLower();
			}
		}

		private string GetBlobName(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				return string.Empty;
			}

			string parentPath = GetContainerName(path);

			if (path.Length > parentPath.Length + 1)
			{
				return path
					.Substring(parentPath.Length + 1)
					.Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
			}
			else
			{
				return string.Empty;
			}
		}
		
		/// <summary>
		/// Add the root folder at the start of the specified path, if a root folder has been set, ensuring that the returned path does
		/// not end with trailing directory separator characters. 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private string UseRootFolder(string path)
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
					return $"{this.RootPath}{System.IO.Path.AltDirectorySeparatorChar}{path}";
				}
				else
				{
					return this.RootPath;
				}
			}
		}

		private string RemoveRootFolder(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				return path;
			}
			else if (String.IsNullOrEmpty(this.RootPath))
			{
				return "";
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

		private string JoinPath(string containerName, string blobName)
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
			 	CanHaveFolders = true,
				CanStoreFiles = false,
				CanRename = false,
				CanDelete = false
			};
		}

		private FileSystemItemCapabilities BuildContainerCapabilities()
		{
			return new FileSystemItemCapabilities()
			{
				CanStoreFiles = true,
				CanRename = false,
				CanDelete = true
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

		private async Task<File> BuildFile(string fileName)
		{
			return new File()
			{
				Provider = this.Key,
				Path = GetRelativePath(fileName),
				Name = GetDisplayName(fileName),
				Parent = await GetFolder(GetRelativePath(GetParentPath(fileName))),
				Capabilities = BuildFileCapabilities()
			};
		}

		private async Task<File> BuildFile(string containerName, BlobItem fileItem)
		{
			return new File()
			{
				Provider = this.Key,
				Path = GetRelativePath(JoinPath(containerName, fileItem.Name)),
				Name = GetDisplayName(fileItem.Name),
				DateModified = fileItem.Properties.LastModified.Value.UtcDateTime,
				Parent = await GetFolder(GetRelativePath(GetParentPath(JoinPath(containerName, fileItem.Name)))),
				Size = fileItem.Properties.ContentLength.Value,
				Capabilities = BuildFileCapabilities()
			};
		}

		private async Task<File> BuildFile(string name, BlobProperties properties)
		{
			return new File()
			{
				Provider = this.Key,
				Path = GetRelativePath(name),
				Name = GetDisplayName(name),
				DateModified = properties.LastModified.UtcDateTime,
				Parent = await GetFolder(GetRelativePath(GetParentPath(name))),
				Size = properties.ContentLength,
				Capabilities = BuildFileCapabilities()
			};
		}


		private Folder BuildFolder(string folderName)
		{
			if (folderName.Equals(this.RootPath, StringComparison.OrdinalIgnoreCase))
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
			else
			{
				return new Folder()
				{
					Provider = this.Key,
					Path = GetRelativePath(folderName),
					Name = GetDisplayName(folderName),
					Parent = new Folder { Provider = this.Key, Path = GetRelativePath(GetParentPath(folderName)) },
					Capabilities = IsContainerPath(folderName) ? BuildContainerCapabilities() : BuildSubFolderCapabilities(),
					FolderValidationRules = BuildSubFolderValidationRules(),
					FileValidationRules = BuildFileValidationRules()
				};
			}
		}

		private Folder BuildFolder(string name, BlobProperties properties)
		{
			return new Folder()
			{
				Provider = this.Key,
				Path = GetRelativePath(name),
				Name = GetDisplayName(name),
				DateModified = properties.LastModified.UtcDateTime,
				//Parent = await GetFolder(GetParentPath(name)),
				Parent = new Folder() { Provider = this.Key, Path = GetRelativePath(GetParentPath(name)) },
				Capabilities = IsContainerPath(name) ? BuildContainerCapabilities() : BuildSubFolderCapabilities(),
				FolderValidationRules = BuildSubFolderValidationRules(),
				FileValidationRules = BuildFileValidationRules()
			};
		}

		private Folder BuildFolder(string containerName, BlobItem fileItem)
		{
			string path = GetRelativePath(JoinPath(containerName, fileItem.Name));

			return new Folder()
			{
				Provider = this.Key,
				Path = path,
				Name = GetDisplayName(fileItem.Name),
				DateModified = fileItem.Properties.LastModified.Value.UtcDateTime,
				Parent = new Folder() { Provider = this.Key, Path = GetRelativePath(GetParentPath(JoinPath(containerName, fileItem.Name))) },
				Capabilities = IsContainerPath(path) ? BuildContainerCapabilities() : BuildSubFolderCapabilities(),
				FolderValidationRules = BuildSubFolderValidationRules(),
				FileValidationRules = BuildFileValidationRules()
			};
		}

		private Folder BuildFolder(BlobContainerItem folderItem)
		{
			return BuildFolder(folderItem.Name, folderItem.Properties);
		}

		private FileSystemValidationRule[] BuildTopFolderValidationRules()
		{
			return new FileSystemValidationRule[]
			{
				new FileSystemValidationRule()
				{
					ValidationExpression = "^[0-9a-z]{1}[0-9a-z-]{2,61}[0-9a-z]$" , ErrorMessage = "Azure top level folders (containers) must start and end with a letter or number, contain only letters, numbers and dashes, and must be lower case.",
				},
				new FileSystemValidationRule()
				{
					ValidationExpression = "^(?!.*--)" , ErrorMessage = "Azure top level folders (containers) must not contain consecutive dashes.",
				}
			};
		}

		private FileSystemValidationRule[] BuildFileValidationRules()
		{
			return new FileSystemValidationRule[]
			{
				new FileSystemValidationRule()
				{
					ValidationExpression = "^.{1,1024}$" , ErrorMessage = "Azure file names must be 1-1024 characters long.",
				},
				new FileSystemValidationRule()
				{
					ValidationExpression = "^[^\\/\\\\]+$" , ErrorMessage = "Azure file names cannot contain the '/' or '\\' character.",
				}
			};
		}


		private FileSystemValidationRule[] BuildSubFolderValidationRules()
		{
			return new FileSystemValidationRule[]
			{
				new FileSystemValidationRule()
				{
					ValidationExpression = "^.{1,1024}$" , ErrorMessage = "Azure sub-folders must be 1-1024 characters long.",
				},
				new FileSystemValidationRule()
				{
					ValidationExpression = "^[^\\/\\\\]+$" , ErrorMessage = "Azure sub-folders cannot contain the '/' or '\\' character.",
				}
			};
		}

		private Folder BuildFolder(string name, BlobContainerProperties properties)
		{
			if (name.Equals(this.RootPath, StringComparison.OrdinalIgnoreCase))
			{
				// top level
				return new Folder()
				{
					Provider = this.Key,
					Path = "",
					Name = "/",
					DateModified = properties.LastModified.UtcDateTime,
					Parent = null, 
					Capabilities = BuildTopFolderCapabilities(),
					FolderValidationRules = BuildTopFolderValidationRules()
				};
			}
			else
			{
				string path = GetRelativePath(name);

				return new Folder()
				{
					Provider = this.Key,
					Path = path,
					Name = GetDisplayName(name),
					DateModified = properties.LastModified.UtcDateTime,
					Parent = new Folder { Provider = this.Key, Path = GetRelativePath(GetParentPath(name)) },
					Capabilities = IsContainerPath(path) ? BuildContainerCapabilities() : BuildSubFolderCapabilities(),
					FolderValidationRules = BuildSubFolderValidationRules(),
					FileValidationRules = BuildFileValidationRules()
				};
			}
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
			path = RemoveRootFolder(path);

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
						// if the path "/" was passed in, return an empty string 
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

		private Boolean ValidateContainerName(string path)
		{
			return !System.Text.RegularExpressions.Regex.IsMatch(path, "[^a-z0-9-]+" );			
		}

		private Boolean IsNotFoundException(Azure.RequestFailedException ex)
		{
			return new string[] { "ContainerNotFound", "BlobNotFound", "InvalidResourceName" }.Contains(ex.ErrorCode, StringComparer.OrdinalIgnoreCase);
		}
	}
}
