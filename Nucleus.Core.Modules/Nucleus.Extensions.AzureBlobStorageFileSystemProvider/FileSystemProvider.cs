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

		public FileSystemProvider()
		{

		}

		public override void Configure(IConfigurationSection configSection, string HomeDirectory)
		{
			configSection.Bind(this.Options);
		}

		public override Folder CreateFolder(string parentPath, string newFolder)
		{
			string fullPath = GetFullPath(parentPath, newFolder);
			BlobServiceClient client = new(this.Options.ConnectionString);

			if (IsContainerPath(GetFullPath(parentPath, newFolder)))
			{
				// folder being created is at the top level, create a container
				client.CreateBlobContainer(GetContainerName(fullPath));
			}
			else
			{
				// folder is "in" a container, create zero-length blob to represent a folder 
				BlobContainerClient containerClient = client.GetBlobContainerClient(GetContainerName(fullPath));
				BlobClient blobClient = containerClient.GetBlobClient(GetBlobName(fullPath));
				blobClient.Upload(new System.IO.MemoryStream());
			}

			return BuildFolder(fullPath);
		}

		public override void DeleteFile(string path)
		{
			BlobServiceClient client = new(this.Options.ConnectionString);
			BlobContainerClient containerClient = client.GetBlobContainerClient(GetContainerName(path));
			containerClient.DeleteBlob(GetBlobName(path));
		}

		public override void DeleteFolder(string path, bool recursive)
		{
			Folder existingFolder = ListFolder(path);

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
							DeleteFile(file.Path);
						}

						foreach (Folder folder in existingFolder.Folders)
						{
							DeleteFolder(folder.Path, recursive);
						}
					}
				}

				BlobServiceClient client = new(this.Options.ConnectionString);
				BlobContainerClient containerClient = client.GetBlobContainerClient(GetContainerName(path));
				containerClient.DeleteBlob(GetBlobName(path));
			}
		}

		public override Abstractions.Models.FileSystem.File GetFile(string path)
		{
			try
			{
				BlobClient blobClient = new(this.Options.ConnectionString, GetContainerName(path), GetBlobName(path));
				Azure.Response<BlobProperties> response = blobClient.GetProperties();
				return BuildFile(path, response.Value);
			}
			catch (Azure.RequestFailedException ex)
			{
				if (ex.ErrorCode == "BlobNotFound")
				{
					throw new System.IO.FileNotFoundException();
				}
				else
				{
					throw;
				}
			}
		}

		public override System.Uri GetFileDirectUrl(string path)
		{
			BlobClient blobClient = new(this.Options.ConnectionString, GetContainerName(path), GetBlobName(path));

			if (blobClient.CanGenerateSasUri)
			{
				try
				{
					return blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(60));
				}
				catch (Azure.RequestFailedException ex)
				{
					if (ex.ErrorCode == "BlobNotFound")
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

		public override System.IO.Stream GetFileContents(string path)
		{
			try
			{
				BlobClient blobClient = new(this.Options.ConnectionString, GetContainerName(path), GetBlobName(path));
				return blobClient.OpenRead();
			}
			catch (Azure.RequestFailedException ex)
			{
				if (ex.ErrorCode == "BlobNotFound")
				{
					throw new System.IO.FileNotFoundException();
				}
				else
				{
					throw;
				}
			}
		}

		public override Folder GetFolder(string path)
		{
			BlobServiceClient client = new(this.Options.ConnectionString);

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
					Azure.Response<BlobContainerProperties> response = containerClient.GetProperties();

					return BuildFolder(path, response.Value);
				}
				else
				{
					// path is a "folder", in a container.  Folders are represented as zero-length blobs
					BlobClient blobClient = new(this.Options.ConnectionString, GetContainerName(path), GetBlobName(path));

					try
					{
						Azure.Response<BlobProperties> response = blobClient.GetProperties();
						return BuildFolder(path, response.Value);
					}
					catch (Azure.RequestFailedException ex)
					{
						if (ex.ErrorCode == "BlobNotFound")
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

		public override Folder ListFolder(string path)
		{
			return ListFolder(path, "");
		}

		public override Folder ListFolder(string path, string pattern)
		{
			BlobServiceClient client = new(this.Options.ConnectionString);
			BlobContainerClient containerClient = client.GetBlobContainerClient(GetContainerName(path));
			Folder folder = BuildFolder(path);

			// In Azure blob storage, containers can only exist at the top level
			if (String.IsNullOrEmpty(path))
			{
				foreach (BlobContainerItem item in client.GetBlobContainers(BlobContainerTraits.None, BlobContainerStates.None, GetBlobName(path)))
				{
					folder.Folders.Add(BuildFolder(item));
				}
			}

			// In Azure blob storage, top level cannot contain files (only containers)
			if (!String.IsNullOrEmpty(path))
			{
				//Azure.Pageable<BlobItem> items = containerClient.GetBlobs(Azure.Storage.Blobs.Models.BlobTraits.None, BlobStates.None, GetBlobName(path));
				string prefix = GetBlobName(path);
				if (!String.IsNullOrEmpty(prefix))
				{
					// if the path is not at the top level of a container (not IsNullOrEmpty) then it has to end with the delimiter, per https://docs.microsoft.com/en-us/dotnet/api/azure.storage.blobs.blobcontainerclient.getblobsbyhierarchy?view=azure-dotnet
					// "The value of a prefix is substring+delimiter"
					prefix += System.IO.Path.AltDirectorySeparatorChar;
				}

				try
				{
					Azure.Pageable<BlobHierarchyItem> items = containerClient.GetBlobsByHierarchy(Azure.Storage.Blobs.Models.BlobTraits.None, BlobStates.None, new string(System.IO.Path.AltDirectorySeparatorChar, 1), prefix);

					foreach (BlobHierarchyItem item in items)
					{
						//GetFullPath(containerClient.Name, item.Prefix)
						if (!item.IsBlob)
						{
							// Ensure that we are not adding the folder to itself
							if (GetFullPath(containerClient.Name, item.Prefix) != path)
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
								if (GetFullPath(containerClient.Name, item.Blob.Name) != path)
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
									folder.Files.Add(BuildFile(containerClient.Name, item.Blob));
								}
							}
						}
					}
				}
				catch (Azure.RequestFailedException ex)
				{
					if (ex.ErrorCode == "BlobNotFound")
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

		public override Abstractions.Models.FileSystem.File RenameFile(string path, string newName)
		{
			throw new NotImplementedException();
		}

		public override Folder RenameFolder(string path, string newName)
		{
			throw new NotImplementedException();
		}

		public override async Task<Abstractions.Models.FileSystem.File> SaveFile(string parentPath, string newFileName, System.IO.Stream content, bool overwrite)
		{
			string newObjectPath = $"{parentPath}{System.IO.Path.AltDirectorySeparatorChar}{newFileName}";
			BlobServiceClient client = new(this.Options.ConnectionString);
			BlobContainerClient containerClient = client.GetBlobContainerClient(GetContainerName(parentPath));
			BlobClient blobClient = containerClient.GetBlobClient(GetBlobName(newObjectPath));

			//await blobClient.UploadAsync(content, overwrite: overwrite);
			Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider extensionProvider = new();

			if (!extensionProvider.TryGetContentType(newFileName, out string mimeType))
			{
				mimeType = "application/octet-stream";
			}

			await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = mimeType, CacheControl = "max-age=3600" });

			return BuildFile(newObjectPath);
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

		private string GetBlobName(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				return string.Empty;
			}

			string parentPath = GetContainerName(path);

			if (path.Length > parentPath.Length + 1)
			{
				return path.Substring(parentPath.Length + 1);
			}
			else
			{
				return string.Empty;
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

		private File BuildFile(string fileName)
		{
			return new File()
			{
				Provider = this.Key,
				Path = GetRelativePath(fileName),
				Name = GetDisplayName(fileName),
				Parent = GetFolder(GetParentPath(fileName)),
				Capabilities = BuildFileCapabilities()
			};
		}

		private File BuildFile(string containerName, BlobItem fileItem)
		{
			return new File()
			{
				Provider = this.Key,
				Path = GetRelativePath(GetFullPath(containerName, fileItem.Name)),
				Name = GetDisplayName(fileItem.Name),
				DateModified = fileItem.Properties.LastModified.Value.UtcDateTime,
				Parent = GetFolder(GetParentPath(GetFullPath(containerName, fileItem.Name))),
				Size = fileItem.Properties.ContentLength.Value,
				Capabilities = BuildFileCapabilities()
			};
		}

		private File BuildFile(string name, BlobProperties properties)
		{
			return new File()
			{
				Provider = this.Key,
				Path = GetRelativePath(name),
				Name = GetDisplayName(name),
				DateModified = properties.LastModified.UtcDateTime,
				Parent = GetFolder(GetParentPath(name)),
				Size = properties.ContentLength,
				Capabilities = BuildFileCapabilities()
			};
		}


		private Folder BuildFolder(string folderName)
		{
			if (folderName.Equals("", StringComparison.OrdinalIgnoreCase))
			{
				// top level
				return new Folder()
				{
					Provider = this.Key,
					Path = "",
					Name = "/",
					//DateModified = folderItem.LastWriteTimeUtc,
					Parent = new Folder() { Provider = this.Key, Path = "" },
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
				Parent = GetFolder(GetParentPath(name)),
				Capabilities = IsContainerPath(name) ? BuildContainerCapabilities() : BuildSubFolderCapabilities(),
				FolderValidationRules = BuildSubFolderValidationRules(),
				FileValidationRules = BuildFileValidationRules()
			};
		}

		private Folder BuildFolder(string containerName, BlobItem fileItem)
		{
			string path = GetRelativePath(GetFullPath(containerName, fileItem.Name));

			return new Folder()
			{
				Provider = this.Key,
				Path = path,
				Name = GetDisplayName(fileItem.Name),
				DateModified = fileItem.Properties.LastModified.Value.UtcDateTime,
				Parent = GetFolder(GetParentPath(GetFullPath(containerName, fileItem.Name))),
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
			if (name.Equals("", StringComparison.OrdinalIgnoreCase))
			{
				// top level
				return new Folder()
				{
					Provider = this.Key,
					Path = "",
					Name = "/",
					DateModified = properties.LastModified.UtcDateTime,
					Parent = new Folder() { Provider = this.Key, Path = "" },
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
