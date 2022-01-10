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
		public override void Configure(IConfigurationSection configSection, string homeDirectory)
		{
			configSection.Bind(this.Options);			

			if (System.IO.Path.IsPathRooted(homeDirectory))
			{
				throw new ArgumentException($"'{homeDirectory}' is not a valid home directory.  The home directory must not start with a backslash or a drive letter.");
			}

			if (!String.IsNullOrEmpty(homeDirectory))
			{
				this.Options.RootFolder = System.IO.Path.Combine(this.FolderOptions.Value.ParseFolder(this.Options.RootFolder), homeDirectory);
			}
			else
			{
				this.Options.RootFolder = this.FolderOptions.Value.ParseFolder(this.Options.RootFolder);
			}

			if (!System.IO.Directory.Exists(this.Options.RootFolder))
			{
				System.IO.Directory.CreateDirectory(this.Options.RootFolder);
			}
		}

		private string BuildPath(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				return this.Options.RootFolder;
			}
			else
			{
				return System.IO.Path.Combine(this.Options.RootFolder, path);
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
				string relativePath = path.Replace(this.Options.RootFolder, "");
				if (relativePath.StartsWith (System.IO.Path.DirectorySeparatorChar) || relativePath.StartsWith(System.IO.Path.AltDirectorySeparatorChar))
				{
					if (relativePath.Length>1)
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

		public override Folder CreateFolder(string parentPath, string newFolder)
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

		public override void DeleteFile(string path)
		{
			if (PathUtils.PathNavigatesAboveRoot(path) || PathUtils.HasInvalidPathChars(path))
			{
				throw new ArgumentException("Invalid path", nameof(path));
			}

			System.IO.File.Delete(BuildPath(path));
		}

		public override void DeleteFolder(string path, Boolean recursive)
		{
			if (PathUtils.PathNavigatesAboveRoot(path) || PathUtils.HasInvalidPathChars(path))
			{
				throw new ArgumentException("Invalid path", nameof(path));
			}

			System.IO.Directory.Delete(BuildPath(path), recursive);
		}

		public override Folder GetFolder(string path)
		{
			if (PathUtils.PathNavigatesAboveRoot(path) || PathUtils.HasInvalidPathChars(path))
			{
				throw new ArgumentException("Invalid path", nameof(path));
			}

			System.IO.DirectoryInfo folderInfo = new(BuildPath(path));

			if (folderInfo.Exists)
			{
				// item is a folder
				return BuildFolder(folderInfo);			
			}
			else
			{
				throw new System.IO.FileNotFoundException();
			}			
		}

		public override File GetFile(string path)
		{
			if (path == null) return null;

			if (PathUtils.PathNavigatesAboveRoot(path) || PathUtils.HasInvalidPathChars(path))
			{
				throw new ArgumentException("Invalid path", nameof(path));
			}

			System.IO.FileInfo fileInfo = new(BuildPath(path));
			if (fileInfo.Exists)
			{
				return BuildFile(fileInfo);
			}
			else
			{
				// file not found
				throw new System.IO.FileNotFoundException();
			}			
		}

		/// <summary>
		/// Get a direct url for the file.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// Returns null to indicate that a direct url could not be returned by the provider.
		/// </remarks>
		public override System.Uri GetFileDirectUrl(string path)
		{
			return null;
		}

		public override System.IO.Stream GetFileContents(string path)
		{
			File file = GetFile(path);
			
			if (file == null)
			{
				throw new System.IO.FileNotFoundException();
			}

			System.IO.FileInfo fileInfo = new(BuildPath(path));
			if (fileInfo.Exists)
			{
				return fileInfo.OpenRead();
			}
			else
			{
				throw new System.IO.FileNotFoundException();
			}
		}

		public override Folder ListFolder(string path)
		{
			return ListFolder(path, "");
		}

		public override Folder ListFolder(string path, string pattern)
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
				.Where(item => String.IsNullOrEmpty(pattern) || System.Text.RegularExpressions.Regex.IsMatch(item.Name, pattern)))
			{
				//if (this.Options.AllowedTypes.Contains(item.Extension, StringComparer.OrdinalIgnoreCase))
				if (this.GlobalOptions.Value.AllowedFileTypes.Where(allowed=>allowed.FileExtensions.Contains(item.Extension, StringComparer.OrdinalIgnoreCase)).Any())
				{
					result.Files.Add(BuildFile(item));
				}
			}

			return result;
		}

		public override File RenameFile(string path, string newName)
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

		public override Folder RenameFolder(string path, string newName)
		{
			string newPath;

			if (PathUtils.PathNavigatesAboveRoot(path) || PathUtils.HasInvalidPathChars(path))
			{
				throw new ArgumentException("Invalid path", nameof(path));
			}

			newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), newName);
			
			System.IO.Directory.Move(BuildPath(path), BuildPath(newPath));						

			return this.GetFolder(newPath);
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

			return GetFile(System.IO.Path.Combine(BuildRelativePath(targetPath)));
		}

		private File BuildFile(System.IO.FileInfo fileItem)
		{
			return new File()
			{
				Provider=this.Key,
				Path = BuildRelativePath(fileItem.FullName),
				Name = fileItem.Name,
				DateModified = fileItem.LastWriteTimeUtc,
				Parent = GetFolder(BuildRelativePath(fileItem.DirectoryName)),// new Folder() { Path = BuildRelativePath(fileItem.DirectoryName) },
				Size = fileItem.Length,
				Capabilities = BuildFileCapabilities()
			};
		}

		private Folder BuildFolder(System.IO.DirectoryInfo folderItem)
		{
			if (folderItem.FullName.Equals(this.Options.RootFolder, StringComparison.OrdinalIgnoreCase))
			{
				// top level
				return new Folder()
				{
					Provider = this.Key,
					Path = "",
					Name = "/",
					DateModified = folderItem.LastWriteTimeUtc,
					Parent = new Folder() { Provider = this.Key, Path = "" },
					Capabilities = BuildFolderCapabilities()
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
					Capabilities = BuildFolderCapabilities(),					
					FolderValidationRules = new FileSystemValidationRule[]
					{
						new FileSystemValidationRule()
						{
							ValidationExpression = "^(?!CON|PRN|AUX|NUL|LPT|COM|).*$" , ErrorMessage = "Folder names cannot start with CON, PRN, AUX, NUL, LPT or COM.",
						},
						new FileSystemValidationRule()
						{
							ValidationExpression = "^(?!COM).*$" , ErrorMessage = "Folder names cannot start with COM.",
						},
						new FileSystemValidationRule()
						{
							ValidationExpression = "^[^<>:\"|?*]+$" , ErrorMessage = "Folder names cannot contain any of the <, >, :, \" | ? or * characters.",
						},
						new FileSystemValidationRule()
						{
							ValidationExpression = "^[^\\/\\\\]+$" , ErrorMessage = "Folder names cannot contain the '/' or '\\' character.",
						}
					},
					FileValidationRules = new FileSystemValidationRule[]
					{
						new FileSystemValidationRule()
						{
							ValidationExpression = "^(?!CON|PRN|AUX|NUL|LPT|COM|).*$" , ErrorMessage = "File names cannot start with CON, PRN, AUX, NUL, LPT or COM.",
						},
						new FileSystemValidationRule()
						{
							ValidationExpression = "^(?!COM).*$" , ErrorMessage = "File names cannot start with COM.",
						},
						new FileSystemValidationRule()
						{
							ValidationExpression = "^[^<>:\"|?*]+$" , ErrorMessage = "File names cannot contain any of the <, >, :, \" | ? or * characters.",
						},
						new FileSystemValidationRule()
						{
							ValidationExpression = "^[^\\/\\\\]+$" , ErrorMessage = "File names cannot contain the '/' or '\\' character.",
						}
					}
				};
			}
		}

		private FileSystemItemCapabilities BuildFolderCapabilities()
		{
			return new FileSystemItemCapabilities()
			{
				CanStoreFiles = true,
				CanRename = true,
				CanDelete = true
			};
		}

		private FileSystemItemCapabilities BuildFileCapabilities()
		{
			return new FileSystemItemCapabilities()
			{
				CanStoreFiles = false,
				CanRename = true,
				CanDelete = true
			};
		}
	}
}
