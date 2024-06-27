using Microsoft.Extensions.Configuration;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Core.FileSystemProviders
{
	/// <summary>
	/// <see cref="FileSystemProvider"/> for the local file system.
	/// </summary>

	// Naming Files, Paths, and Namespaces (Win32)
	// https://docs.microsoft.com/en-us/windows/win32/fileio/naming-a-file#file-and-directory-names


	public class LocalFileSystemProvider : FileSystemProvider
	{
		private LocalFileSystemProviderOptions Options { get; } = new();
		private IOptions<FileSystemProviderFactoryOptions> GlobalOptions { get; }
		private IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> FolderOptions { get; }

		public LocalFileSystemProvider(IOptions<FileSystemProviderFactoryOptions> globalOptions, IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions> folderOptions)
		{
			this.GlobalOptions = globalOptions;
			this.FolderOptions = folderOptions;
		}

		/// <summary>
		/// Sets the configuration for this instance.
		/// </summary>
		/// <param name="configSection"></param>
		public override void Configure(IConfigurationSection configSection, string rootDirectory)
		{
			configSection.Bind(this.Options);
			
			if (String.IsNullOrEmpty(this.Options.RootPath))
			{
				this.Options.RootPath = this.FolderOptions.Value.GetDataFolder("Content", true);
			}			
			
			// Home directory is not used
			//if (System.IO.Path.IsPathRooted(rootDirectory))
			//{
			//	throw new ArgumentException($"'{rootDirectory}' is not a valid root directory.  The home directory must not start with a backslash or a drive letter.");
			//}
			//if (!String.IsNullOrEmpty(rootDirectory))
			//{
			//	this.Options.RootFolder = System.IO.Path.Combine(this.FolderOptions.Value.ParseFolder(this.Options.RootFolder), rootDirectory);
			//}
			//else
			//{
				this.Options.RootPath = this.FolderOptions.Value.ParseFolder(this.Options.RootPath);
			//}

			if (!System.IO.Directory.Exists(this.Options.RootPath))
			{
				System.IO.Directory.CreateDirectory(this.Options.RootPath);
			}
		}

		private string BuildPath(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				return this.Options.RootPath;
			}
			else
			{
				return Nucleus.Abstractions.Models.Configuration.FolderOptions.NormalizePath(System.IO.Path.Combine(this.Options.RootPath, path));
			}
		}

		private string BuildRelativePath(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				return path;
			}
			else
			{
        //string relativePath = path.Replace(this.Options.RootPath, "").Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        string relativePath = Nucleus.Abstractions.Models.Configuration.FolderOptions.NormalizePath(path).Replace(this.Options.RootPath, "");
        
        if (relativePath.StartsWith('/'))
				{
					if (relativePath.Length > 1)
					{
						// remove leading "/"
						return relativePath[1..];
					}
					else
					{
						// if the path "/" was passed in, return an empty string (for the LocalFileSystemProvider, an empty string is the "root" path)
						return "";
					}
				}
				else
				{
					return relativePath;
				}
			}
		}

		public override Task<Folder> CreateFolder(string parentPath, string newFolder)
		{
			if (PathUtils.PathNavigatesAboveRoot(parentPath) || PathUtils.HasInvalidPathChars(parentPath))
			{
				throw new ArgumentException("Invalid parent path.", nameof(parentPath));
			}

			if (PathUtils.HasInvalidFileChars(newFolder) || PathUtils.HasInvalidPathChars(newFolder))
			{
				throw new ArgumentException("Invalid folder name.", nameof(newFolder));
			}

			System.IO.Directory.CreateDirectory(System.IO.Path.Combine(BuildPath(parentPath), newFolder));
			return this.GetFolder(System.IO.Path.Combine(parentPath, newFolder));
		}

		public override Task DeleteFile(string path)
		{
			if (PathUtils.PathNavigatesAboveRoot(path) || PathUtils.HasInvalidPathChars(path))
			{
				throw new ArgumentException("Invalid path", nameof(path));
			}

			System.IO.File.Delete(BuildPath(path));

			return Task.CompletedTask;
		}

		public override Task DeleteFolder(string path, Boolean recursive)
		{
			if (PathUtils.PathNavigatesAboveRoot(path) || PathUtils.HasInvalidPathChars(path))
			{
				throw new ArgumentException("Invalid path", nameof(path));
			}

			System.IO.Directory.Delete(BuildPath(path), recursive);

			return Task.CompletedTask;
		}

		public override Task<Folder> GetFolder(string path)
		{
			if (PathUtils.PathNavigatesAboveRoot(path) || PathUtils.HasInvalidPathChars(path))
			{
				throw new ArgumentException("Invalid path", nameof(path));
			}

			System.IO.DirectoryInfo folderInfo = new(BuildPath(path));

			if (folderInfo.Exists)
			{
				// item is a folder
				return Task.FromResult(BuildFolder(folderInfo));
			}
			else
			{
				throw new System.IO.FileNotFoundException($"Could not find folder '{path}'.", path);
			}
		}

		public override Task<File> GetFile(string path)
		{
			if (path == null) return null;

			if (PathUtils.PathNavigatesAboveRoot(path) || PathUtils.HasInvalidPathChars(path))
			{
				throw new ArgumentException("Invalid path", nameof(path));
			}

			System.IO.FileInfo fileInfo = new(BuildPath(path));
			if (fileInfo.Exists)
			{
				return Task.FromResult(BuildFile(fileInfo));
			}
			else
			{
				// file not found
				throw new System.IO.FileNotFoundException($"Could not find file '{path}'.", path);
			}
		}

		/// <summary>
		/// Get a direct url for the file.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// Returns null to indicate that a direct url could not be returned by the provider.
		/// </remarks>
		public override Task<System.Uri> GetFileDirectUrl(string path, DateTime expiresOn)
		{
			return Task.FromResult<System.Uri>(null);
		}

		public override async Task<System.IO.Stream> GetFileContents(string path)
		{
			File file = await GetFile(path);

			if (file == null)
			{
				throw new System.IO.FileNotFoundException($"Could not find file '{path}'.", path);
			}

			System.IO.FileInfo fileInfo = new(BuildPath(path));
			if (fileInfo.Exists)
			{
				return fileInfo.OpenRead();
			}
			else
			{
				throw new System.IO.FileNotFoundException($"Could not find file '{path}'.", path);
			}
		}

		public override Task<Folder> ListFolder(string path)
		{
			return ListFolder(path, "");
		}

		public override Task<Folder> ListFolder(string path, string pattern)
		{
			if (PathUtils.PathNavigatesAboveRoot(path) || PathUtils.HasInvalidPathChars(path))
			{
				throw new ArgumentException("Invalid path", nameof(path));
			}

			System.IO.DirectoryInfo folderInfo = new(BuildPath(path));
			Folder result = BuildFolder(folderInfo);

			foreach (System.IO.DirectoryInfo subFolderInfo in folderInfo.GetDirectories())
			{
				result.Folders.Add(BuildFolder(subFolderInfo));
			}

			foreach (System.IO.FileInfo item in folderInfo.GetFiles()
				.Where(item => String.IsNullOrEmpty(pattern) || System.Text.RegularExpressions.Regex.IsMatch(item.Name, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase)))
			{
				//if (this.Options.AllowedTypes.Contains(item.Extension, StringComparer.OrdinalIgnoreCase))
				if (this.GlobalOptions.Value.AllowedFileTypes.Where(allowed => allowed.FileExtensions.Contains(item.Extension, StringComparer.OrdinalIgnoreCase)).Any())
				{
					result.Files.Add(BuildFile(item));
				}
			}

			return Task.FromResult(result);
		}

		public override Task<File> RenameFile(string path, string newName)
		{
			string newPath;

			if (PathUtils.PathNavigatesAboveRoot(path) || PathUtils.HasInvalidPathChars(path))
			{
				throw new ArgumentException("Invalid path", nameof(path));
			}

			if (System.IO.Path.GetExtension(newName) != System.IO.Path.GetExtension(path))
			{
				throw new ArgumentException("Changing the file extension is not allowed.", nameof(newName));
			}

			if (!this.GlobalOptions.Value.AllowedFileTypes.Where(allowed => allowed.FileExtensions.Contains(System.IO.Path.GetExtension(newName), StringComparer.OrdinalIgnoreCase)).Any())
			{
				throw new ArgumentException("Unsupported file extension", nameof(newName));
			}

			newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), newName);
			System.IO.File.Move(BuildPath(path), BuildPath(newPath));

			return this.GetFile(newPath);
		}

		public override async Task<Folder> RenameFolder(string path, string newName)
		{
			string newPath;

			if (PathUtils.PathNavigatesAboveRoot(path) || PathUtils.HasInvalidPathChars(path))
			{
				throw new ArgumentException("Invalid path", nameof(path));
			}

			newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), newName);

			System.IO.Directory.Move(BuildPath(path), BuildPath(newPath));

			return await this.GetFolder(newPath);
		}

		public async override Task<File> SaveFile(string parentPath, string newFileName, System.IO.Stream content, Boolean overwrite)
		{
			if (PathUtils.PathNavigatesAboveRoot(parentPath) || PathUtils.HasInvalidPathChars(parentPath))
			{
				throw new ArgumentException("Invalid Path.");
			}

			if (PathUtils.HasInvalidFileChars(newFileName) || PathUtils.HasInvalidPathChars(newFileName))
			{
				throw new ArgumentException("Unsupported file name.");
			}

			//if (!this.Options.AllowedTypes.Contains(System.IO.Path.GetExtension(newFileName), StringComparer.OrdinalIgnoreCase))
			if (!this.GlobalOptions.Value.AllowedFileTypes.Where(allowed => allowed.FileExtensions.Contains(System.IO.Path.GetExtension(newFileName), StringComparer.OrdinalIgnoreCase)).Any())
			{
				throw new ArgumentException("Unsupported file type.");
			}

			string targetPath = System.IO.Path.Combine(BuildPath(parentPath), newFileName);

			if (!overwrite && System.IO.File.Exists(targetPath))
			{
				throw new System.IO.IOException("The file already exists.");
			}

			using (System.IO.Stream outStream = System.IO.File.Open(targetPath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
			{
				await content.CopyToAsync(outStream);
			}

			return await GetFile(System.IO.Path.Combine(BuildRelativePath(targetPath)));
		}

		private File BuildFile(System.IO.FileInfo fileItem)
		{
			return new File()
			{
				Provider = this.Key,
				Path = BuildRelativePath(fileItem.FullName),
				Name = fileItem.Name,
				DateModified = fileItem.LastWriteTimeUtc,
				Parent = new Folder() { Provider = this.Key, Path = BuildRelativePath(fileItem.DirectoryName) },
				Size = fileItem.Length,
				Capabilities = LocalFileCapabilities
			};
		}

		private Folder BuildFolder(System.IO.DirectoryInfo folderItem)
		{
			if (folderItem.Parent.FullName.Equals(this.Options.RootPath, StringComparison.OrdinalIgnoreCase))
			{
				// top level
				return new Folder()
				{
					Provider = this.Key,
					Path = "",
					Name = "/",
					DateModified = folderItem.LastWriteTimeUtc,
					Parent = null, 
					Capabilities = LocalFolderCapabilities,
					FolderValidationRules = LocalFolderValidationRules,
					FileValidationRules = LocalFileValidationRules
				};
			}
			else
			{
				return new Folder()
				{
					Provider = this.Key,
					Path = BuildRelativePath(folderItem.FullName),
					Name = folderItem.Name,
					DateModified = folderItem.LastWriteTimeUtc,
					Parent = new Folder { Provider = this.Key, Path = BuildRelativePath(folderItem.Parent.FullName) },
					Capabilities = LocalFolderCapabilities,
					FolderValidationRules = LocalFolderValidationRules,
					FileValidationRules = LocalFileValidationRules
				};
			}
		}

		private static FileSystemValidationRule[] LocalFolderValidationRules = new FileSystemValidationRule[]
		{
			new FileSystemValidationRule()
			{
				ValidationExpression = "^(?!CON|PRN|AUX|NUL|LPT|COM)" , ErrorMessage = "Folder names cannot start with CON, PRN, AUX, NUL, LPT or COM.",
			},
			new FileSystemValidationRule()
			{
				ValidationExpression = "^[^<>:\"|?*]+$" , ErrorMessage = "Folder names cannot contain any of the <, >, :, \" | ? or * characters.",
			},
			new FileSystemValidationRule()
			{
				ValidationExpression = "^[^\\/\\\\]+$" , ErrorMessage = "Folder names cannot contain the '/' or '\\' character.",
			}
		};
		

		private static FileSystemValidationRule[] LocalFileValidationRules = new FileSystemValidationRule[]
		{
			new FileSystemValidationRule()
			{
				ValidationExpression = "^(?!CON|PRN|AUX|NUL|LPT|COM)" , ErrorMessage = "File names cannot start with CON, PRN, AUX, NUL, LPT or COM.",
			},
			new FileSystemValidationRule()
			{
				ValidationExpression = "^[^<>:\"|?*]+$" , ErrorMessage = "File names cannot contain any of the <, >, :, \" | ? or * characters.",
			},
			new FileSystemValidationRule()
			{
				ValidationExpression = "^[^\\/\\\\]+$" , ErrorMessage = "File names cannot contain the '/' or '\\' character.",
			}
		};
		

		private static FileSystemItemCapabilities LocalFolderCapabilities = new FileSystemItemCapabilities()
		{
			CanStoreFiles = true,
			CanRename = true,
			CanDelete = true
		};

		private static FileSystemItemCapabilities LocalFileCapabilities = new FileSystemItemCapabilities()
		{
			CanStoreFiles = false,
			CanRename = true,
			CanDelete = true
		};
		
	}
}
