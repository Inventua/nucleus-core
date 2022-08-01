using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Core;
using Nucleus.Data.Common;
using Nucleus.Core.DataProviders;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Core.FileSystemProviders;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Core.Managers
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Folder"/>s.
	/// </summary>
	public class FileSystemManager : IFileSystemManager
	{
		private ICacheManager CacheManager { get; }
		private IDataProviderFactory DataProviderFactory { get; }
		private IFileSystemProviderFactory FileSystemProviderFactory { get; }
		//private Context Context { get; }

		public FileSystemManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager, IFileSystemProviderFactory fileSystemProviderFactory)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
			this.FileSystemProviderFactory = fileSystemProviderFactory;
		}

		/// <summary>
		/// Retrieve an existing <see cref="Folder"/> from the database.
		/// </summary>
		/// <returns></returns>
		public async Task<Folder> GetFolder(Site site, string providerName, string path)
		{
			using (IFileSystemDataProvider provider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				Folder folder = this.FileSystemProviderFactory.Get(site, providerName).GetFolder(path);

				if (folder != null)
				{
					Folder folderData = await provider.GetFolder(site, providerName, path);
					if (folderData == null)
					{
						// database entry does not exist, create 
						folderData = await provider.SaveFolder(site, folder);
					}

					if (folderData != null)
					{
						folderData.CopyDatabaseValuesTo(folder);						
						return folder;
					}
				}

				return null;
			}
		}

		/// <summary>
		/// Retrieve an existing <see cref="Folder"/> from the database.
		/// </summary>
		/// <returns></returns>
		public async Task<Folder> GetFolder(Site site, Guid id)
		{
			using (IFileSystemDataProvider provider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				Folder folderData = await provider.GetFolder(id);

				if (folderData != null)
				{
					Folder folder = this.FileSystemProviderFactory.Get(site, folderData.Provider).GetFolder(folderData.Path);
					folderData.CopyDatabaseValuesTo(folder);

					return folder;
				}
			}

			throw new System.IO.FileNotFoundException();

		}

		/// <summary>
		/// Retrieve an existing <see cref="File"/> from the database.
		/// </summary>
		/// <returns></returns>
		public async Task<File> GetFile(Site site, Guid id)
		{
			if (id == Guid.Empty) return null;

			using (IFileSystemDataProvider provider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				File fileData = await provider.GetFile(id);
				
				if (fileData != null)
				{
					File file = this.FileSystemProviderFactory.Get(site, fileData.Provider).GetFile(fileData.Path);
					fileData.CopyDatabaseValuesTo(file);

					await GetDatabaseProperties(site, file.Parent);
					return file;
				}
			}

			throw new System.IO.FileNotFoundException();
		}

		/// <summary>
		/// Retrieve an existing <see cref="File"/> from the database, or create a new record if none exists.
		/// </summary>
		/// <returns></returns>
		public async Task<File> GetFile(Site site, string providerName, string path)
		{
			using (IFileSystemDataProvider provider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				File file = this.FileSystemProviderFactory.Get(site, providerName).GetFile(path);

				if (file != null)
				{
					File fileData = await provider.GetFile(site, providerName, path);
					if (fileData == null)
					{
						// database entry does not exist, create 
						fileData = await provider.SaveFile(site, new File() { Provider = providerName, Path = path });
					}

					if (fileData != null)
					{
						fileData.CopyDatabaseValuesTo(file);
						await GetDatabaseProperties(site, file.Parent);
						return file;
					}
				}
			}

			return null;
		}


		/// <summary>
		/// Create/add default permissions to the specified <see cref="Folder"/> for the specified <see cref="Role"/>.
		/// </summary>
		/// <param name="folder"></param>
		/// <param name="role"></param>
		/// <remarks>
		/// The new <see cref="Permission"/>s are not saved unless you call <see cref="SaveFolderPermissions(Folder)"/>.
		/// </remarks>
		public async Task CreatePermissions(Site site, Folder folder, Role role)
		{
			Boolean isAnonymousOrAllUsers = role.Equals(site.AnonymousUsersRole) || role.Equals(site.AllUsersRole);

			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<PermissionType> permissionTypes = await provider.ListPermissionTypes(Folder.URN);
				List<Permission> permissions = new();

				foreach (PermissionType permissionType in permissionTypes)
				{
					Permission permission = new();
					permission.Role = role;

					if (isAnonymousOrAllUsers && !permissionType.IsFolderViewPermission())
					{
						permission.AllowAccess = false;
						permission.PermissionType = new() { Scope = PermissionType.PermissionScopeNamespaces.Disabled };
					}
					else
					{
						permission.AllowAccess = permissionType.IsFolderViewPermission();
						permission.PermissionType = permissionType;
					}

					permissions.Add(permission);
				}

				folder.Permissions.AddRange(permissions);
			}
		}

		/// <summary>
		/// Save permissions for the specified <see cref="Folder"/>.
		/// </summary>
		/// <param name="folder"></param>
		/// <remarks>
		/// A side-effect of saving folder permissions is that a database record is created for the folder, if one does not already exist.
		/// </remarks>
		public async Task SaveFolderPermissions(Site site, Folder folder)
		{
			if (folder.Id == Guid.Empty)
			{
				using (IFileSystemDataProvider provider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
				{
					Folder existing = await provider.GetFolder(site, folder.Provider, folder.Path);

					if (existing == null)
					{
						// create a new folder record
						await provider.SaveFolder(site, folder);
					}
					else
					{
						existing.Permissions = folder.Permissions;
						folder = existing;
					}
				}
			}

			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<Permission> originalPermissions = await provider.ListPermissions(folder.Id, Folder.URN);

				await provider.SavePermissions(folder.Id, folder.Permissions, originalPermissions);
				this.CacheManager.FolderCache().Remove(folder.Id);
			}
		}

		/// <summary>
		/// List all permissions for the folder specified by folderId.
		/// </summary>
		/// <param name="folderId"></param>
		/// <returns></returns>
		public async Task<List<Permission>> ListPermissions(Folder folder)
		{
			List<Permission> result = new();

			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<PermissionType> permissionTypes = await provider.ListPermissionTypes(Folder.URN);
				List<Permission> permissions = await provider.ListPermissions(folder.Id, Folder.URN);

				// ensure that for each role with any permissions defined, there is a full set of permission types for the role
				foreach (Role role in permissions.Select((permission) => permission.Role).ToList())
				{
					foreach (PermissionType permissionType in permissionTypes)
					{
						if (permissions.Where((permission) => permission?.Role.Id == role.Id && permission?.PermissionType.Id == permissionType.Id).ToList().Count == 0)
						{
							Permission permission = new();
							permission.AllowAccess = false;
							permission.PermissionType = permissionType;
							permission.Role = role;
							permissions.Add(permission);
						}
					}
				}

				result = permissions.OrderBy((permission) => permission.Role.Name).ThenBy((permission) => permission.PermissionType.SortOrder).ToList();
			}

			return result;
		}

		/// <summary>
		/// Return a list of available permission types, sorted by SortOrder
		/// </summary>
		/// <returns></returns>
		public async Task<List<PermissionType>> ListFolderPermissionTypes()
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				return (await provider.ListPermissionTypes(Folder.URN)).OrderBy(permissionType => permissionType.SortOrder).ToList();
			}
		}

		private Boolean IsValidFolderName(Folder parentFolder, string name, ref string message)
		{
			if (parentFolder.FolderValidationRules != null)
			{
				foreach (FileSystemValidationRule rule in parentFolder.FolderValidationRules)
				{
					if (!System.Text.RegularExpressions.Regex.IsMatch(name, rule.ValidationExpression))
					{
						message = rule.ErrorMessage;
						return false;
					}
				}
			}

			return true;
		}

		private Boolean IsValidFileName(Folder parentFolder, string name, ref string message)
		{
			if (parentFolder.FileValidationRules != null)
			{
				foreach (FileSystemValidationRule rule in parentFolder.FileValidationRules)
				{
					if (!System.Text.RegularExpressions.Regex.IsMatch(name, rule.ValidationExpression))
					{
						message = rule.ErrorMessage;
						return false;
					}
				}
			}

			return true;
		}

		public async Task<Folder> CreateFolder(Site site, string providerName, string parentPath, string newFolder)
		{
			// create the physical folder
			FileSystemProvider fileSystemProvider = this.FileSystemProviderFactory.Get(site, providerName);
			Folder parentFolder = fileSystemProvider.GetFolder(parentPath);
			string message = "";

			if (!IsValidFolderName(parentFolder, newFolder, ref message))
			{
				throw new InvalidOperationException(message);
			}

			Folder result = fileSystemProvider.CreateFolder(parentPath, newFolder);

			// SaveFolderPermissions creates a record in the database for the folder (a new folder doesn't have
			// any permissions to save, but we do want to save the database record).
			await SaveFolderPermissions(site, result);

			return result;
		}

		public async Task DeleteFolder(Site site, Folder folder, Boolean recursive)
		{
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, folder.Provider);
			provider.DeleteFolder(folder.Path, recursive);

			using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				await dataProvider.DeleteFolder(folder);
				this.CacheManager.FolderCache().Remove(folder.Id);
			}
		}

		public async Task RenameFolder(Site site, Folder folder, string newName)
		{
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, folder.Provider);

			folder = provider.RenameFolder(folder.Path, newName);
			Folder parentFolder = provider.GetFolder(folder.Parent.Path);
			string message = "";

			if (!IsValidFolderName(parentFolder, newName, ref message))
			{
				throw new InvalidOperationException(message);
			}

			using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				Folder existing = await dataProvider.GetFolder(folder.Id);

				if (existing != null)
				{
					folder.Id = existing.Id;
					await dataProvider.SaveFolder(site, folder);
					this.CacheManager.FolderCache().Remove(folder.Id);

					if (parentFolder != null)
					{
						this.CacheManager.FolderCache().Remove(parentFolder.Id);
					}
				}
			}
		}

		public IReadOnlyList<FileSystemProviderInfo> ListProviders()
		{
			return this.FileSystemProviderFactory.Providers;
		}

		/// <summary>
		/// Return a folder with the Folders and Files properties populated.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="id"></param>
		/// <param name="pattern">Regular expression which is used to filter file names.</param>
		/// <returns></returns>
		/// <example>
		/// ListFolder(this.Context.Site, folderId, "(.xml)");
		/// </example>
		public async Task<Folder> ListFolder(Site site, Guid id, string pattern)
		{
			Folder existingFolder = await this.GetFolder(site, id);
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, existingFolder.Provider);

			Folder folder = provider.ListFolder(existingFolder.Path, pattern);

			await GetDatabaseProperties(site, folder);
			await GetDatabaseProperties(site, folder.Parent);
			await GetDatabaseProperties(site, folder.Folders);
			await GetDatabaseProperties(site, folder.Files);
			return folder;
		}

		private async Task GetDatabaseProperties(Site site, List<File> files)
		{
			foreach (File file in files)
			{
				await GetDatabaseProperties(site, file);
			}
		}

		private async Task<File> GetDatabaseProperties(Site site, File file)
		{
			File fileData = await this.GetFile(site, file.Provider, file.Path);
			if (fileData != null)
			{
				fileData.CopyDatabaseValuesTo(file);				
			}
			return file;
		}

		private async Task GetDatabaseProperties(Site site, List<Folder> folders)
		{
			foreach (Folder folder in folders)
			{
				await GetDatabaseProperties(site, folder);
			}
		}

		private async Task<Folder> GetDatabaseProperties(Site site, Folder folder)
		{
			Folder folderData = await this.GetFolder(site, folder.Provider, folder.Path);
			if (folderData != null)
			{
				folderData.CopyDatabaseValuesTo(folder);
			}
			return folder;
		}

		public async Task<System.Uri> GetFileDirectUrl(Site site, File file)
		{
			if (!String.IsNullOrEmpty(file.DirectUrl) && (!file.DirectUrlExpiry.HasValue || file.DirectUrlExpiry.Value > DateTime.UtcNow))
			{
				if (System.Uri.TryCreate(file.DirectUrl, UriKind.Absolute, out Uri uri))
				{
					return uri;
				}
			}

			DateTime expiresOn = DateTime.UtcNow.AddDays(30);
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, file.Provider);
			System.Uri directUrl = provider.GetFileDirectUrl(file.Path, expiresOn);

			if (directUrl != null)
			{
				file.DirectUrl = directUrl.ToString();
				file.DirectUrlExpiry = expiresOn;

				using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
				{
					await dataProvider.SaveFile(site, file);
				}
			}

			return directUrl;
		}

		public System.IO.Stream GetFileContents(Site site, File file)
		{
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, file.Provider);
			return provider.GetFileContents(file.Path);
		}

		public async Task DeleteFile(Site site, File file)
		{
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, file.Provider);
			Folder parentFolder = provider.GetFolder(file.Parent.Path);

			provider.DeleteFile(file.Path);

			using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				await dataProvider.DeleteFile(file);

				if (parentFolder != null)
				{
					this.CacheManager.FolderCache().Remove(parentFolder.Id);
				}
			}
		}

		public async Task RenameFile(Site site, File file, string newName)
		{
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, file.Provider);
			Folder parentFolder = provider.GetFolder(file.Parent.Path);
			string message = "";

			if (!IsValidFileName(parentFolder, newName, ref message))
			{
				throw new InvalidOperationException(message);
			}

			if (!System.IO.Path.GetExtension(file.Name).Equals(System.IO.Path.GetExtension(newName), StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Changing the file extension is not allowed.");
			}

			File newfile = provider.RenameFile(file.Path, newName);

			using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				file.Path = newfile.Path;
				await dataProvider.SaveFile(site, file);
			}

			if (parentFolder != null)
			{
				this.CacheManager.FolderCache().Remove(parentFolder.Id);
			}
		}

		public async Task<Folder> SaveFolder(Site site, Folder folder)
		{
			using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				return await dataProvider.SaveFolder(site, folder);
			}
		}

		public async Task<File> SaveFile(Site site, string providerName, string parentPath, string newFileName, System.IO.Stream content, Boolean overwrite)
		{
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, providerName);

			if (content != null)
			{
				Folder parentFolder = provider.GetFolder(parentPath);
				string message = "";

				if (!IsValidFileName(parentFolder, newFileName, ref message))
				{
					throw new InvalidOperationException(message);
				}

				File file = await provider.SaveFile(parentPath, newFileName, content, overwrite);
				await GetDatabaseProperties(site, file);

				// try to get the image dimensions and save them.  The GetImageDimensions extension checks that the file is
				// an image and does nothing if it is not, so we don't need to check that here. 
				file.GetImageDimensions(site, this);
				
				using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
				{
					await dataProvider.SaveFile(site, file);
				}

				return file;
			}
			else
			{
				throw new ArgumentException("Input stream is null");
			}
		}

		public async Task<File> RefreshProperties(Site site, File file)
		{
			try
			{
				if (file != null)
				{
					if (file.Id != Guid.Empty)
					{
						file = await this.GetFile(site, file.Id);
					}

					if (file.Parent != null)
					{
						file.Parent.Permissions = await this.ListPermissions(file.Parent);
					}
					else
					{
						file.Parent = await this.GetFolder(site, file.Provider ?? this.ListProviders().FirstOrDefault()?.Key, "");
					}
				}
			}
			catch (System.IO.FileNotFoundException)
			{
				// in case file has been deleted
			}

			return file;
		}
	}
}
