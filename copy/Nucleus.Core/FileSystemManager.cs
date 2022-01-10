using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Core.DataProviders.Abstractions;
using Nucleus.Core.DataProviders;
using Nucleus.Core.FileSystemProviders;
using System.Collections.ObjectModel;

namespace Nucleus.Core
{
	/// <summary>
	/// Provides functions to manage database data for <see cref="Folder"/>s.
	/// </summary>
	public class FileSystemManager
	{
		private CacheManager CacheManager { get; }
		private DataProviderFactory DataProviderFactory { get; }
		private FileSystemProviderFactory FileSystemProviderFactory { get; }
		//private Context Context { get; }

		public FileSystemManager(DataProviderFactory dataProviderFactory, CacheManager cacheManager, FileSystemProviderFactory fileSystemProviderFactory)
		{
			this.CacheManager = cacheManager;
			this.DataProviderFactory = dataProviderFactory;
			this.FileSystemProviderFactory = fileSystemProviderFactory;
		}

		/// <summary>
		/// Retrieve an existing <see cref="Folder"/> from the database.
		/// </summary>
		/// <returns></returns>
		public Folder GetFolder(Site site, string providerName, string path)
		{
			using (IFileSystemDataProvider provider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				Folder folder = this.FileSystemProviderFactory.Get(site, providerName).GetFolder(path);

				if (folder != null)
				{
					Folder folderData = provider.GetFolder(site, providerName, path);
					if (folderData == null)
					{
						// database entry does not exist, create 
						folderData = provider.SaveFolder(site, folder);
					}

					if (folderData != null)
					{
						folder.Id = folderData.Id;
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
		public Folder GetFolder(Site site, Guid id)
		{
			using (IFileSystemDataProvider provider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				Folder folderData = provider.GetFolder(id);

				if (folderData != null)
				{
					Folder folder = this.FileSystemProviderFactory.Get(site, folderData.Provider).GetFolder(folderData.Path);
					folder.Id = folderData.Id;
					return folder;
				}
			}

			throw new System.IO.FileNotFoundException();

		}

		/// <summary>
		/// Retrieve an existing <see cref="File"/> from the database.
		/// </summary>
		/// <returns></returns>
		public File GetFile(Site site, Guid id)
		{
			if (id == Guid.Empty) return null;

			using (IFileSystemDataProvider provider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				File fileData = provider.GetFile(id);

				if (fileData != null)
				{
					File file = this.FileSystemProviderFactory.Get(site, fileData.Provider).GetFile(fileData.Path);
					file.Id = fileData.Id;
					GetDatabaseId(site, file.Parent);
					return file;
				}
			}

			throw new System.IO.FileNotFoundException();
		}

		/// <summary>
		/// Retrieve an existing <see cref="File"/> from the database, or create a new record if none exists.
		/// </summary>
		/// <returns></returns>
		public File GetFile(Site site, string providerName, string path)
		{
			using (IFileSystemDataProvider provider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				File file = this.FileSystemProviderFactory.Get(site, providerName).GetFile(path);

				if (file != null)
				{
					File fileData = provider.GetFile(site, providerName, path);
					if (fileData == null)
					{
						// database entry does not exist, create 
						fileData = provider.SaveFile(site, new File() { Provider = providerName, Path = path });
					}

					if (fileData != null)
					{
						file.Id = fileData.Id;
						GetDatabaseId(site, file.Parent);
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
		public void CreatePermissions(Folder folder, Role role)
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<PermissionType> permissionTypes = provider.ListFolderPermissionTypes();
				List<Permission> permissions = new();

				foreach (PermissionType permissionType in permissionTypes)
				{
					Permission permission = new();
					permission.AllowAccess = false;
					permission.PermissionType = permissionType;
					permission.Role = role;

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
		public void SaveFolderPermissions(Site site, Folder folder)
		{
			if (folder.Id == Guid.Empty)
			{
				using (IFileSystemDataProvider provider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
				{
					Folder existing = provider.GetFolder(site, folder.Provider, folder.Path);

					if (existing == null)
					{
						// create a new folder record
						provider.SaveFolder(site, folder);
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
				List<Permission> originalPermissions = provider.ListFolderPermissions(folder.Id);

				provider.SaveFolderPermissions(folder.Id, folder.Permissions, originalPermissions);
				this.CacheManager.FolderCache.Remove(folder.Id);
			}
		}

		/// <summary>
		/// List all permissions for the folder specified by folderId.
		/// </summary>
		/// <param name="folderId"></param>
		/// <returns></returns>
		public List<Permission> ListPermissions(Folder folder)
		{
			List<Permission> result = new();

			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<PermissionType> permissionTypes = provider.ListFolderPermissionTypes();
				List<Permission> permissions = provider.ListFolderPermissions(folder.Id);

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
		public List<PermissionType> ListFolderPermissionTypes()
		{
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				return provider.ListFolderPermissionTypes().OrderBy(permissionType => permissionType.SortOrder).ToList();
			}
		}

		public Folder CreateFolder(Site site, string providerName, string parentPath, string newFolder)
		{
			// create the physical folder
			FileSystemProvider fileSystemProvider = this.FileSystemProviderFactory.Get(site, providerName);
			Folder result = fileSystemProvider.CreateFolder(parentPath, newFolder);

			// Create default permissions

			// Add permissions (administrator-view/edit)				
			using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
			{
				List<PermissionType> permissionTypes = provider.ListModulePermissionTypes();
				List<Permission> permissions = new();

				foreach (PermissionType permissionType in permissionTypes)
				{
					Permission permission = new();
					permission.AllowAccess = true;
					permission.PermissionType = permissionType;
					permission.Role = site.AdministratorsRole;

					permissions.Add(permission);
				}

				result.Permissions.AddRange(permissions);
			}

			SaveFolderPermissions(site, result);

			return result;
		}

		public void DeleteFolder(Site site, Folder folder, Boolean recursive)
		{
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, folder.Provider);
			provider.DeleteFolder(folder.Path, recursive);

			using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				dataProvider.DeleteFolder(folder);
				this.CacheManager.FolderCache.Remove(folder.Id);
			}
		}

		public void RenameFolder(Site site, string providerName, string path, string newName)
		{
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, providerName);
			
			Folder folder = provider.RenameFolder(path, newName);
			Folder parentFolder = provider.GetFolder(folder.Parent.Path);

			using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				Folder existing = dataProvider.GetFolder(site, providerName, path);

				if (existing != null)
				{
					folder.Id = existing.Id;
					dataProvider.SaveFolder(site, folder);
					this.CacheManager.FolderCache.Remove(folder.Id);

					if (parentFolder != null)
					{
						this.CacheManager.FolderCache.Remove(parentFolder.Id);
					}
				}
			}
		}

		public ReadOnlyDictionary<string, FileSystemProviderInfo> ListProviders()
		{
			return this.FileSystemProviderFactory.Providers;
		}

		public Folder ListFolder(Site site, string providerName, string path, string pattern)
		{
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, providerName);
			Folder folder = provider.ListFolder(path, pattern);
			GetDatabaseId(site, folder);
			GetDatabaseId(site, folder.Folders);
			GetDatabaseId(site, folder.Files);
			return folder;
		}

		public Folder ListFolder(Site site, Guid id, string pattern)
		{
			Folder existingFolder = this.GetFolder(site, id);
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, existingFolder.Provider);

			Folder folder = provider.ListFolder(existingFolder.Path, pattern);

			GetDatabaseId(site, folder);
			GetDatabaseId(site, folder.Folders);
			GetDatabaseId(site, folder.Files);
			return folder;
		}

		public Folder ListFolder(Site site, string providerName, string path)
		{
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, providerName);
			Folder folder = provider.ListFolder(path);

			GetDatabaseId(site, folder);
			GetDatabaseId(site, folder.Folders);
			GetDatabaseId(site, folder.Files);
			return folder;
		}

		private void GetDatabaseId(Site site, List<File> files)
		{
			foreach (File file in files)
			{
				GetDatabaseId(site, file);
			}
		}

		private File GetDatabaseId(Site site, File file)
		{
			File fileData = this.GetFile(site, file.Provider, file.Path);
			if (fileData != null)
			{
				file.Id = fileData.Id;
			}
			return file;
		}

		private void GetDatabaseId(Site site, List<Folder> folders)
		{
			foreach (Folder folder in folders)
			{
				GetDatabaseId(site, folder);
			}
		}

		private Folder GetDatabaseId(Site site, Folder folder)
		{
			Folder folderData = this.GetFolder(site, folder.Provider, folder.Path);
			if (folderData != null)
			{
				folder.Id = folderData.Id;
			}
			return folder;
		}

		

		public System.IO.Stream GetFileContents(Site site, string providerName, string path)
		{
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, providerName);
			return provider.GetFileContents(path);
		}

		public void DeleteFile(Site site, File file)
		{
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, file.Provider);
			Folder parentFolder = provider.GetFolder(file.Parent.Path);

			provider.DeleteFile(file.Path);

			using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				dataProvider.DeleteFile(file);
				
				if (parentFolder != null)
				{
					this.CacheManager.FolderCache.Remove(parentFolder.Id);
				}				
			}
		}

		public void RenameFile(Site site, File file, string newName)
		{
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, file.Provider);
			Folder parentFolder = provider.GetFolder(file.Parent.Path);

			File newfile = provider.RenameFile(file.Path, newName);

			using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				file.Path = newfile.Path;
				dataProvider.SaveFile(site, file);
			}

			if (parentFolder != null)
			{
				this.CacheManager.FolderCache.Remove(parentFolder.Id);
			}
		}

		public async Task<File> SaveFile(Site site, string providerName, string parentPath, string newFileName, System.IO.Stream content, Boolean overwrite)
		{
			FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, providerName);

			if (content != null)
			{
				File file = await provider.SaveFile(parentPath, newFileName, content, overwrite);

				GetDatabaseId(site, file);
				return file;
			}
			else
			{
				throw new ArgumentException("Input stream is null");
			}
		}

	}
}
