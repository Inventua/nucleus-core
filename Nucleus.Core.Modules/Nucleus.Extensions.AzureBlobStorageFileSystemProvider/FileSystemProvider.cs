using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions.Models.FileSystem;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

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
			PathUri path = new PathUri(this.RootPath, parentPath, PathUri.AddDelimiter(newFolder));
			//string fullPath = JoinPath(UseRootFolder(parentPath), newFolder);
			BlobServiceClient client = new(this.Options.ConnectionString);

			if (path.PathUriType == PathUri.PathUriTypes.Root)
			{
				if (!ValidateContainerName(path.ContainerName))
				{
					throw new InvalidOperationException($"Cannot create folder '{newFolder}' because it contains spaces or other invalid characters.  Top level folders (containers) in Azure Storage must be lower case, and contain only letters, numbers and dashes.");
				}
				// folder being created is at the top level, create a container
				await client.CreateBlobContainerAsync(path.ContainerName);
			}
			else
			{
				// folder is "in" a container, create zero-length blob to represent a folder 
				BlobContainerClient containerClient = client.GetBlobContainerClient(path.ContainerName);
				BlobClient blobClient = containerClient.GetBlobClient(path.Key);
				await blobClient.UploadAsync(new System.IO.MemoryStream());
			}

			return BuildFolder(path);
		}

		public override async Task DeleteFile(string path)
		{
			PathUri pathUri = new(this.RootPath, path);
			BlobServiceClient client = new(this.Options.ConnectionString);
			BlobContainerClient containerClient = client.GetBlobContainerClient(pathUri.ContainerName);
			await containerClient.DeleteBlobAsync(pathUri.Key);
		}

		public override async Task DeleteFolder(string path, bool recursive)
		{
			PathUri pathUri = new(this.RootPath, path, true);
			Folder existingFolder = await ListFolder(path);

			if (pathUri.PathUriType == PathUri.PathUriTypes.Container)
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
				BlobContainerClient containerClient = client.GetBlobContainerClient(pathUri.ContainerName);
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
				BlobContainerClient containerClient = client.GetBlobContainerClient(pathUri.ContainerName);
				containerClient.DeleteBlob(pathUri.Key);
			}
		}

		public override async Task<Abstractions.Models.FileSystem.File> GetFile(string path)
		{
			PathUri pathUri = new(this.RootPath, path);

			try
			{
				BlobClient blobClient = new(this.Options.ConnectionString, pathUri.ContainerName, pathUri.Key);
				Azure.Response<BlobProperties> response = await blobClient.GetPropertiesAsync();
				return BuildFile(path, response.Value);
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
			PathUri pathUri = new(this.RootPath, path);
			// set cache expiry to 30 minutes less than the SAS token expiry so a browser doesn't ever try a direct url that won't work
			long maxAge = (Int64)((expiresOn - DateTime.UtcNow).TotalHours - 0.5) * 60;
			BlobClient blobClient = new(this.Options.ConnectionString, pathUri.ContainerName, pathUri.Key);
			
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
			PathUri pathUri = new(this.RootPath, path);

			try
			{
				BlobClient blobClient = new(this.Options.ConnectionString, pathUri.ContainerName, pathUri.Key);
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
			PathUri pathUri = new(this.RootPath, path, true); 
			BlobServiceClient client = new(this.Options.ConnectionString);			

			if (pathUri.PathUriType == PathUri.PathUriTypes.Root)
			{
				return BuildFolder(pathUri);  
			}
			else
			{
				if (pathUri.PathUriType == PathUri.PathUriTypes.Container)
				{
					// Path is a top level container
					BlobContainerClient containerClient = client.GetBlobContainerClient(pathUri.ContainerName);

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
					BlobClient blobClient = new(this.Options.ConnectionString, pathUri.ContainerName, pathUri.Key);

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
			PathUri pathUri = new(this.RootPath, path, true); 
			BlobServiceClient client = new(this.Options.ConnectionString);
			
			BlobContainerClient containerClient = client.GetBlobContainerClient(pathUri.ContainerName);
			Folder folder = BuildFolder(pathUri);

			// In Azure blob storage, containers can only exist at the top level
			if (pathUri.PathUriType == PathUri.PathUriTypes.Root)
			{
				await foreach (BlobContainerItem item in client.GetBlobContainersAsync(BlobContainerTraits.None, BlobContainerStates.None, pathUri.Key))
				{
					folder.Folders.Add(BuildFolder(item));
				}
			}

			// In Azure blob storage, top level cannot contain files (only containers)
			if (pathUri.PathUriType != PathUri.PathUriTypes.Root)
			{
				string prefix = pathUri.Key;

				if (pathUri.PathUriType != PathUri.PathUriTypes.Container)
				{
					// if the path is not at the top level of a container (not IsNullOrEmpty) then it has to end with the delimiter, per https://docs.microsoft.com/en-us/dotnet/api/azure.storage.blobs.blobcontainerclient.getblobsbyhierarchy?view=azure-dotnet
					// "The value of a prefix is substring+delimiter"
					prefix = PathUri.AddDelimiter(prefix);
				}

				try
				{
					await foreach (BlobHierarchyItem item in containerClient.GetBlobsByHierarchyAsync(Azure.Storage.Blobs.Models.BlobTraits.None, BlobStates.None, PathUri.PATH_DELIMITER.ToString(), prefix))
					{
						if (!item.IsBlob)
						{
							// Ensure that we are not adding the folder to itself
							if (JoinPath(containerClient.Name, item.Prefix) != path)
							{
								folder.Folders.Add(BuildFolder(new PathUri(this.RootPath, PathUri.AddDelimiter(containerClient.Name) + item.Prefix, true)));
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
									// When a folder contains files, .GetBlobsByHierarchy will have already returned it as an item with
									// .IsBlob set to false, so we have to check that the folder hasn't already been added to the result
									// before adding it.
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
								if (String.IsNullOrEmpty(pattern) || System.Text.RegularExpressions.Regex.IsMatch(item?.Blob.Name, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
								{
									folder.Files.Add(BuildFile(containerClient.Name, item.Blob));
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
			PathUri pathUri = new(this.RootPath, JoinPath(parentPath, newFileName));
      
			BlobServiceClient client = new(this.Options.ConnectionString);
			BlobContainerClient containerClient = client.GetBlobContainerClient(pathUri.ContainerName);
			BlobClient blobClient = containerClient.GetBlobClient(pathUri.Key);

			Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider extensionProvider = new();

			if (!extensionProvider.TryGetContentType(newFileName, out string mimeType))
			{
				mimeType = "application/octet-stream";
			}

			await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = mimeType, CacheControl = "max-age=3600" });

			//return BuildFile(newObjectPath);
			return BuildFile(pathUri);
		}
			

		private string JoinPath(string containerName, string blobName)
		{
			if (!String.IsNullOrEmpty(containerName))
			{
				return $"{containerName}{PathUri.PATH_DELIMITER}{blobName}";
			}
			else
			{
				return blobName;
			}
		}

		private File BuildFile(PathUri path)
		{
			return new File()
			{
				Provider = this.Key,
				Path = path.RelativePath,
				Name = path.DisplayName,
				Parent = new Folder() { Provider = this.Key, Path = path.Parent.RelativePath },
				Capabilities = FileSystemCapabilities.File
			};
		}

		private File BuildFile(string containerName, BlobItem fileItem)
		{
			PathUri path = new(this.RootPath, containerName, fileItem.Name);
			File file = BuildFile(path);
			file.DateModified = fileItem.Properties.LastModified.Value.UtcDateTime;
			file.Size = fileItem.Properties.ContentLength.Value;
			return file;
		}

		private File BuildFile(string name, BlobProperties properties)
		{
			PathUri path = new(this.RootPath, name);
			File file = BuildFile(path);
			file.DateModified = properties.LastModified.UtcDateTime;
			file.Size = properties.ContentLength;
			return file;
		}


		private Folder BuildFolder(PathUri path)
		{
			//if (folderName.Equals(this.RootPath, StringComparison.OrdinalIgnoreCase) || String.IsNullOrEmpty(GetRelativePath(GetParentPath(folderName))))
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
			else if (path.PathUriType == PathUri.PathUriTypes.Container)
			{
				// top level
				return new Folder()
				{
					Provider = this.Key,
					Path = path.ContainerName,
					Name = path.ContainerName,
          Parent = new Folder { Provider = this.Key, Path = path.Parent.RelativePath },
          Capabilities = FileSystemCapabilities.Container,
					FolderValidationRules = FileSystemValidationRules.Container
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

		private Folder BuildFolder(string name, BlobProperties properties)
		{
			PathUri path = new(this.RootPath, name, true);
			Folder folder = BuildFolder(path);
			folder.DateModified = properties.LastModified.UtcDateTime;
			return folder;
		}

		private Folder BuildFolder(string containerName, BlobItem fileItem)
		{
			PathUri path = new(this.RootPath, containerName, PathUri.AddDelimiter(fileItem.Name));
			return BuildFolder(path);
		}

		private Folder BuildFolder(BlobContainerItem folderItem)
		{
			return BuildFolder(folderItem.Name, folderItem.Properties);
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
					Capabilities = FileSystemCapabilities.Root,
					FolderValidationRules = FileSystemValidationRules.Root
				};
			}
			else
			{
				PathUri path = new(this.RootPath, name, true);
				Folder folder = BuildFolder(path);
				folder.DateModified = properties.LastModified.UtcDateTime;
				return folder;
			}
		}


		private Boolean ValidateContainerName(string path)
		{
			return !System.Text.RegularExpressions.Regex.IsMatch(path, "[^a-z0-9-]+" );			
		}


		private static string[] AZURE_NOT_FOUND_CODES = new string[] { "ContainerNotFound", "BlobNotFound", "InvalidResourceName" };

		private Boolean IsNotFoundException(Azure.RequestFailedException ex)
		{
			return AZURE_NOT_FOUND_CODES.Contains(ex.ErrorCode, StringComparer.OrdinalIgnoreCase);
		}



	}
}
