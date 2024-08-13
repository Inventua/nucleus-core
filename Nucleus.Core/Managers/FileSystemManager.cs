using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Models.Paging;
using Nucleus.Core.DataProviders;
using Nucleus.Data.Common;
using Nucleus.Extensions;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Core.Managers;

/// <summary>
/// Provides functions to manage database data for <see cref="Folder"/>s.
/// </summary>
public class FileSystemManager : IFileSystemManager
{
	private ICacheManager CacheManager { get; }
	private IDataProviderFactory DataProviderFactory { get; }
	private IFileSystemProviderFactory FileSystemProviderFactory { get; }
	private IPermissionsManager PermissionsManager { get; }

	private static char[] DirectorySeparators = new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar };

	public FileSystemManager(IDataProviderFactory dataProviderFactory, ICacheManager cacheManager, IPermissionsManager permissionsManager, IFileSystemProviderFactory fileSystemProviderFactory)
	{
		this.CacheManager = cacheManager;
		this.DataProviderFactory = dataProviderFactory;
		this.FileSystemProviderFactory = fileSystemProviderFactory;
		this.PermissionsManager = permissionsManager;
	}

	private string FileSystemCachePath(Site site, string providerName, string path)
	{
		return $"{site.Id}|{providerName}|{path}";
	}

	/// <summary>
	/// Retrieve an existing <see cref="Folder"/> from the database.
	/// </summary>
	/// <returns></returns>
	public async Task<Folder> GetFolder(Site site, string providerName, string path)
	{
		Guid id = await this.CacheManager.FolderPathCache().GetAsync(FileSystemCachePath(site, providerName, path), async key =>
		{
			using (IFileSystemDataProvider provider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				FileSystemProvider fileSystemProvider = this.FileSystemProviderFactory.Get(site, providerName);
				Folder folder = null;

				// Get folder information from the file system
				try
				{
					folder = RemoveSiteHomeDirectory(site, await fileSystemProvider.GetFolder(UseSiteHomeDirectory(site, path)));
				}
				catch (System.IO.FileNotFoundException)
				{
					// if the root folder was not found, try to create it
					if (String.IsNullOrEmpty(path) || path == "\\" || path == "/")
					{
						folder = RemoveSiteHomeDirectory(site, await fileSystemProvider.CreateFolder("", UseSiteHomeDirectory(site, "")));
					}
				}

				// Retrieve folder information from the database.  We only use Id (see below)
				Folder databaseEntry = await provider.GetFolder(site, providerName, path);

				if (databaseEntry == null && folder != null)
				{
					// database entry does not exist for the folder found in the file system, create database record
					databaseEntry = await provider.SaveFolder(site, folder);
				}

				if (databaseEntry != null)
				{
					return databaseEntry.Id;
				}

				return default;
			}
		});

		return await GetFolder(site, id);
	}

	/// <summary>
	/// Retrieve an existing <see cref="Folder"/> from the database.
	/// </summary>
	/// <returns></returns>
	public async Task<Folder> GetFolder(Site site, Guid id)
	{
		using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
		{
			Folder folderData = await this.CacheManager.FolderCache().GetAsync(id, async id =>
			{
				Folder newCacheEntry = await dataProvider.GetFolder(id);

				// populate permissions so that future retrievals from the cache will always have the permissions list populated
				if (newCacheEntry != null)
				{
					newCacheEntry.Permissions = await this.PermissionsManager.ListPermissions(newCacheEntry.Id, Folder.URN);

					FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, newCacheEntry.Provider);
					Folder folder = await provider.GetFolder(UseSiteHomeDirectory(site, newCacheEntry.Path));
					folder = RemoveSiteHomeDirectory(site, folder);

					newCacheEntry.CopyDatabaseValuesTo(folder);

					if (folder.Parent != null)
					{
						if (!String.IsNullOrEmpty(site.HomeDirectory) && (String.IsNullOrEmpty(folder.Path) || folder.Path.Equals(site.HomeDirectory, StringComparison.OrdinalIgnoreCase)))
						{
							// prevent site's root folder from being part of the folder tree
							folder.Parent = null;
						}
						else
						{
							RemoveSiteHomeDirectory(site, folder.Parent);
							Folder parent = RemoveSiteHomeDirectory(site, await dataProvider.GetFolder(site, folder.Provider, folder.Parent.Path));
							parent?.CopyDatabaseValuesTo(folder.Parent);
						}
					}

					return folder;
				}
				else
				{
					return null;
				}
			});

			if (folderData != null)
			{
				return folderData;
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
			//File fileData = await provider.GetFile(id);
			return await this.CacheManager.FileCache().GetAsync(id, async id =>
			{
				File databaseEntry = await provider.GetFile(id);

				if (databaseEntry != null)
				{
					File file = RemoveSiteHomeDirectory(site, await this.FileSystemProviderFactory.Get(site, databaseEntry.Provider).GetFile(UseSiteHomeDirectory(site, databaseEntry.Path)));
					databaseEntry.CopyDatabaseValuesTo(file);

					await GetDatabaseProperties(site, file.Parent);

					// Fully populate the parent so that the cached version is ready for use anywhere
					if (file.Parent != null)
					{
						if (!String.IsNullOrEmpty(site.HomeDirectory) && file.Parent.Path.Equals(site.HomeDirectory, StringComparison.OrdinalIgnoreCase))
						{
							// prevent site's root folder from being part of the folder tree
							file.Parent = null;
						}
						else
						{
							file.Parent = await this.GetFolder(site, file.Parent.Id);
						}
					}

					return file;
				}
				else
				{
					throw new System.IO.FileNotFoundException();
				}
			});
		}
	}

	/// <summary>
	/// Retrieve an existing <see cref="File"/> from the database, or create a new record if none exists.
	/// </summary>
	/// <returns></returns>
	public async Task<File> GetFile(Site site, string providerName, string path)
	{
		Guid id = await this.CacheManager.FilePathCache().GetAsync(FileSystemCachePath(site, providerName, path), async key =>
		{
			using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				// get file information from the file system
				File file = RemoveSiteHomeDirectory(site, await this.FileSystemProviderFactory.Get(site, providerName).GetFile(UseSiteHomeDirectory(site, path)));

				if (file != null)
				{
					// read file data from the database
					File fileData = await dataProvider.GetFile(site, file.Provider, file.Path);

					if (fileData == null)
					{
						// database entry does not exist, create 

						// for newly-detected files, try to get the image dimensions.  The GetImageDimensions extension checks that
						// the file is an image and does nothing if it is not, so we don't need to check that here. 
						await file.GetImageDimensions(site, this);
						fileData = await dataProvider.SaveFile(site, file);
					}

					if (fileData != null)
					{
						return fileData.Id;
					}
				}
			}

			return default;
		});

		return await GetFile(site, id);
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

		List<PermissionType> permissionTypes = await this.PermissionsManager.ListPermissionTypes(Folder.URN);
		List<Permission> permissions = new();

		foreach (PermissionType permissionType in permissionTypes)
		{
			Permission permission = new();
			permission.Role = role;

			if (isAnonymousOrAllUsers && permissionType.IsFolderEditPermission())
			{
				permission.AllowAccess = false;
        permission.IsDisabled = true;
        permission.PermissionType = permissionType;// new() { Scope = PermissionType.PermissionScopeNamespaces.Disabled };
			}
			else
			{
				permission.AllowAccess = permissionType.IsFolderViewPermission();
				permission.PermissionType = permissionType;
			}

			permissions.Add(permission);
		}

		// only add new permissions if they don't already exist for the folder
		foreach (Permission newPermission in permissions)
		{
			Permission existingPermission = folder.Permissions.Where(existingPermission => existingPermission.Role.Id == role.Id && existingPermission.PermissionType.Scope == newPermission.PermissionType.Scope).FirstOrDefault();
			if (existingPermission == null)
			{
				// only add new permissions if there isn't already a matching permission for the folder      
				folder.Permissions.Add(newPermission);
			}
			else
			{
				// otherwise, update the existing one
				existingPermission.AllowAccess = newPermission.AllowAccess;
			}
		}
		//folder.Permissions.AddRange(permissions);
	}
	//}

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
					//existing.Permissions = folder.Permissions;
					folder.Id = existing.Id;
				}
			}
		}

		List<Permission> originalPermissions = await this.PermissionsManager.ListPermissions(folder.Id, Folder.URN);
		await this.PermissionsManager.SavePermissions(folder.Id, folder.Permissions, originalPermissions);

		this.CacheManager.FolderCache().Remove(folder.Id);
		this.CacheManager.FolderPathCache().Remove(FileSystemCachePath(site, folder.Provider, folder.Path));
	}

	/// <summary>
	/// List all permissions for the folder specified by folderId.
	/// </summary>
	/// <param name="folderId"></param>
	/// <returns></returns>
	public async Task<List<Permission>> ListPermissions(Folder folder)
	{
		List<Permission> result = new();

		//using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
		//{
		List<PermissionType> permissionTypes = await this.PermissionsManager.ListPermissionTypes(Folder.URN);
		List<Permission> permissions = await this.PermissionsManager.ListPermissions(folder.Id, Folder.URN);

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
		//}

		return result;
	}

	/// <summary>
	/// Return a list of available permission types, sorted by SortOrder
	/// </summary>
	/// <returns></returns>
	public async Task<List<PermissionType>> ListFolderPermissionTypes()
	{
		//using (IPermissionsDataProvider provider = this.DataProviderFactory.CreateProvider<IPermissionsDataProvider>())
		//{
		return (await this.PermissionsManager.ListPermissionTypes(Folder.URN)).OrderBy(permissionType => permissionType.SortOrder).ToList();
		//}
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
    Folder rootFolder;
    Folder parentFolder;
		string message = "";

    try
    {
      rootFolder = await fileSystemProvider.GetFolder(UseSiteHomeDirectory(site, "/"));
    }
    catch (System.IO.FileNotFoundException)
    {
      // if the root folder was not found, try to create it				
      rootFolder = RemoveSiteHomeDirectory(site, await fileSystemProvider.CreateFolder("", UseSiteHomeDirectory(site, "")));
    }

    parentFolder = await fileSystemProvider.GetFolder(UseSiteHomeDirectory(site, parentPath));
    
		if (!IsValidFolderName(parentFolder, newFolder, ref message))
		{
			throw new InvalidOperationException(message);
		}

		Folder result = RemoveSiteHomeDirectory(site, await fileSystemProvider.CreateFolder(UseSiteHomeDirectory(site, parentPath), newFolder));

		// SaveFolderPermissions creates a record in the database for the folder (a new folder doesn't have
		// any permissions to save, but we do want to save the database record).
		await SaveFolderPermissions(site, result);

    // re-load the new folder to ensure that it is fully populated
    result = await this.GetFolder(site, result.Id);

		return result;
	}

	public async Task DeleteFolder(Site site, Folder folder, Boolean recursive)
	{
		FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, folder.Provider);
		await provider.DeleteFolder(UseSiteHomeDirectory(site, folder.Path), recursive);

		using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
		{
			await dataProvider.DeleteFolder(folder);
		}

		this.CacheManager.FolderCache().Remove(folder.Id);
		this.CacheManager.FolderPathCache().Remove(FileSystemCachePath(site, folder.Provider, folder.Path));

		Folder parentFolder = await this.GetFolder(site, folder.Provider, folder.Parent.Path);

		if (parentFolder != null)
		{
			this.CacheManager.FolderCache().Remove(parentFolder.Id);
			this.CacheManager.FolderPathCache().Remove(FileSystemCachePath(site, parentFolder.Provider, parentFolder.Path));
		}
	}

	public async Task RenameFolder(Site site, Folder originalFolder, string newName)
	{
		FileSystemProvider fileSystemProvider = this.FileSystemProviderFactory.Get(site, originalFolder.Provider);
		Folder renamedFolder = RemoveSiteHomeDirectory(site, await fileSystemProvider.RenameFolder(UseSiteHomeDirectory(site, originalFolder.Path), newName));
		// renamedFolder won't have an ID, because the FileSystemProvider doesn't know anything about database entries
		originalFolder.CopyDatabaseValuesTo(renamedFolder);

		// we can't guarantee that originalFolder has a populated parent property, and .RenameFolder doesn't populate it, so we must
		// read the parent folder here, so that we can use the Id to remove parent cache entries.
		Folder parentFolder = await this.GetFolder(site, originalFolder.Provider, originalFolder.Parent.Path);
		string message = "";

		if (!IsValidFolderName(parentFolder, newName, ref message))
		{
			throw new InvalidOperationException(message);
		}

		using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
		{
			await dataProvider.SaveFolder(site, renamedFolder);
			this.CacheManager.FolderCache().Remove(renamedFolder.Id);
			this.CacheManager.FolderPathCache().Remove(FileSystemCachePath(site, originalFolder.Provider, originalFolder.Path));

			if (parentFolder != null)
			{
				this.CacheManager.FolderCache().Remove(parentFolder.Id);
				this.CacheManager.FolderPathCache().Remove(FileSystemCachePath(site, parentFolder.Provider, parentFolder.Path));
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
		Folder folder;
		FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, existingFolder.Provider);

		try
		{
			folder = await provider.ListFolder(UseSiteHomeDirectory(site, existingFolder.Path), pattern);
		}
		catch (System.IO.FileNotFoundException)
		{
			// if the folder was not found, try to create it				
			folder = await provider.CreateFolder("", UseSiteHomeDirectory(site, existingFolder.Path));
		}

		await GetDatabaseProperties(site, folder);

		foreach (Folder subfolder in folder.Folders)
		{
			subfolder.Parent = folder;
		}

		foreach (File file in folder.Files)
		{
			file.Parent = folder;
		}

		await GetDatabaseProperties(site, folder.Parent);
		//await GetDatabaseProperties(site, folder.Folders);
		//await GetDatabaseProperties(site, folder.Files);

		return folder;
	}

	/// <summary>
	/// Return a folder with the Folders and Files properties populated, checking user permissions.
	/// </summary>
	/// <param name="site"></param>
	/// <param name="id"></param>
	/// <param name="user"></param>
	/// <param name="pattern">Regular expression which is used to filter file names.</param>
	/// <returns></returns>
	/// <example>
	/// ListFolder(this.Context.Site, folderId, "(.xml)");
	/// </example>
	public async Task<Folder> ListFolder(Site site, Guid id, ClaimsPrincipal user, string pattern)
	{
		Folder existingFolder = await this.GetFolder(site, id);
		Folder folder;
		FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, existingFolder.Provider);

		try
		{
			folder = await provider.ListFolder(UseSiteHomeDirectory(site, existingFolder.Path), pattern);
		}
		catch (System.IO.FileNotFoundException)
		{
			// if the folder was not found, try to create it				
			folder = await provider.CreateFolder("", UseSiteHomeDirectory(site, existingFolder.Path));
		}

		await GetDatabaseProperties(site, folder);



		// we must get database properties for folders in order to retrieve permissions before calling CheckFolderPermission
		await GetDatabaseProperties(site, folder.Parent);
		//await GetDatabaseProperties(site, folder.Folders);
		//await GetDatabaseProperties(site, folder.Files);

		CheckFolderPermissions(site, user, folder);

		return folder;
	}

	/// <summary>
	/// Return a paged list of <seealso cref="Nucleus.Abstractions.Models.FileSystem.FileSystemItem"/> objects for the 
	/// specified folder.
	/// </summary>
	/// <param name="site"></param>
	/// <param name="id"></param>
	/// <param name="pattern">Regular expression which is used to filter file names.</param>
	/// <param name="settings">Paging settings.</param>
	/// <returns></returns>
	public async Task<PagedResult<FileSystemItem>> ListFolder(Site site, Guid id, string pattern, PagingSettings settings)
	{
		Folder existingFolder = await this.GetFolder(site, id);
		Folder folder;
		FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, existingFolder.Provider);
		PagedResult<FileSystemItem> results;

		try
		{
			folder = await provider.ListFolder(UseSiteHomeDirectory(site, existingFolder.Path), pattern);
		}
		catch (System.IO.FileNotFoundException)
		{
			// if the folder was not found, try to create it				
			folder = await provider.CreateFolder("", UseSiteHomeDirectory(site, existingFolder.Path));
		}

		await GetDatabaseProperties(site, folder);
		await GetDatabaseProperties(site, folder.Parent);

		List<FileSystemItem> items = new List<FileSystemItem>();
		folder.SortFolders(folder => folder.Name, false);
		folder.SortFiles(file => file.Name, false);

		items.AddRange(folder.Folders);
		items.AddRange(folder.Files);

		results = new(settings,
			items
				.Skip(settings.FirstRowIndex)
				.Take(settings.PageSize)
		.ToList());

		results.TotalCount = items.Count;

		foreach (FileSystemItem item in results.Items)
		{
			if (item is Folder)
			{
				Folder subfolder = item as Folder;
				subfolder.Parent = folder;
				await GetDatabaseProperties(site, subfolder);
			}

			if (item is File)
			{
				File file = item as File;
				file.Parent = folder;

				await GetDatabaseProperties(site, file);
			}
		}

		return results;
	}

	/// <summary>
	/// Return a paged list of <seealso cref="Nucleus.Abstractions.Models.FileSystem.FileSystemItem"/> objects for the 
	/// specified folder, checking user permissions.
	/// </summary>
	/// <param name="site"></param>
	/// <param name="id"></param>
	/// <param name="user"></param>
	/// <param name="pattern">Regular expression which is used to filter file names.</param>
	/// <param name="settings">Paging settings.</param>
	/// <returns></returns>
	public async Task<PagedResult<FileSystemItem>> ListFolder(Site site, Guid id, ClaimsPrincipal user, string pattern, PagingSettings settings)
	{
		Folder existingFolder = await this.GetFolder(site, id);
		Folder folder;
		FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, existingFolder.Provider);
		PagedResult<FileSystemItem> results;

		try
		{
			folder = await provider.ListFolder(UseSiteHomeDirectory(site, existingFolder.Path), pattern);
		}
		catch (System.IO.FileNotFoundException)
		{
			// if the folder was not found, try to create it				
			folder = await provider.CreateFolder("", UseSiteHomeDirectory(site, existingFolder.Path));
		}

		// we must get database properties for folders in order to retrieve permissions before calling CheckFolderPermission
		await GetDatabaseProperties(site, folder);
		//await GetDatabaseProperties(site, folder.Folders);      
		await GetDatabaseProperties(site, folder.Parent);

		CheckFolderPermissions(site, user, folder);

		List<FileSystemItem> items = new List<FileSystemItem>();
		folder.SortFolders(folder => folder.Name, false);
		folder.SortFiles(file => file.Name, false);

		items.AddRange(folder.Folders);
		items.AddRange(folder.Files);

		results = new(settings,
			items
				.Skip(settings.FirstRowIndex)
				.Take(settings.PageSize)
		.ToList());

		results.TotalCount = items.Count;

		// This logic was moved to GetDatabaseProperties(site, folder)
		////foreach (FileSystemItem item in results.Items)
		////{
		////  if (item is Folder)
		////  {
		////    Folder subfolder = item as Folder;
		////    subfolder.Parent = folder;
		////  }

		////  if (item is File)
		////  {
		////    File file = item as File;
		////    file.Parent = folder;

		////    await GetDatabaseProperties(site, file);
		////  }
		////}

		return results;
	}

	/// <summary>
	/// Check that the user has permission for subfolders/files, and filter those where the user does not have permission.
	/// </summary>
	/// <param name="site"></param>
	/// <param name="user"></param>
	/// <param name="folder"></param>
	private void CheckFolderPermissions(Site site, ClaimsPrincipal user, Folder folder)
	{
		// user must have browse permission on the folder to see contents (files and folders)
		if (!user.HasBrowsePermission(site, folder))
		{
			folder.Files.Clear();
			folder.Folders.Clear();
		}

		// user must have browse permission on individual sub-folders to see them
		foreach (Folder subFolder in folder.Folders.ToList())
		{
			if (!user.HasBrowsePermission(site, subFolder))
			{
				folder.Folders.Remove(subFolder);
			}
		}
	}

	// Moved to GetDatabaseProperties(Site site, Folder folder)
	//  private async Task GetDatabaseProperties(Site site, List<File> files)
	//{
	//    foreach (File file in files)
	//	{
	//		await GetDatabaseProperties(site, file);
	//	}
	//}

	private async Task<File> GetDatabaseProperties(Site site, File file)
	{
		if (file == null) return null;

		RemoveSiteHomeDirectory(site, file);

		File fileData = await this.GetFile(site, file.Provider, file.Path);
		if (fileData != null)
		{
			fileData.CopyDatabaseValuesTo(file);
		}
		return file;
	}

	// Moved to GetDatabaseProperties(Site site, Folder folder)
	//private async Task GetDatabaseProperties(Site site, List<Folder> folders)
	//{
	//	foreach (Folder folder in folders)
	//	{
	//		await GetDatabaseProperties(site, folder);
	//	}
	//}

	private async Task<Folder> GetDatabaseProperties(Site site, Folder folder)
	{
		List<Folder> databaseFolders;
		List<File> databaseFiles;

		if (folder == null) return null;

		RemoveSiteHomeDirectory(site, folder);
		Folder folderData = await this.GetFolder(site, folder.Provider, folder.Path);
		if (folderData != null)
		{
			folderData.CopyDatabaseValuesTo(folder);
		}

		using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
		{
			databaseFolders = await dataProvider.ListFolders(site, folder.Provider, folder.Path);
		}

		foreach (Folder subFolder in folder.Folders)
		{
			RemoveSiteHomeDirectory(site, subFolder);

			Folder databaseFolder = databaseFolders.Where(databaseFile => databaseFile.Path.Equals(subFolder.Path, StringComparison.OrdinalIgnoreCase))
				.FirstOrDefault();

			if (databaseFolder != null)
			{
				databaseFolder.CopyDatabaseValuesTo(subFolder);
			}
			else
			{
				// folder is not in the database, create a database entry
				using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
				{
					await dataProvider.SaveFolder(site, subFolder);
				}
			}

			subFolder.Parent = folder;
		}

		using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
		{
			databaseFiles = await dataProvider.ListFiles(site, folder.Provider, folder.Path);
		}

		foreach (File file in folder.Files)
		{
			RemoveSiteHomeDirectory(site, file);

			File databaseFile = databaseFiles.Where(databaseFile => databaseFile.Path.Equals(file.Path, StringComparison.OrdinalIgnoreCase))
				.FirstOrDefault();

			if (databaseFile != null)
			{
				databaseFile.CopyDatabaseValuesTo(file);
			}
			else
			{
				// file is not in the database, create a database entry
				await file.GetImageDimensions(site, this);
				using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
				{
					await dataProvider.SaveFile(site, file);
				}
			}

			file.Parent = folder;
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
		System.Uri directUrl = await provider.GetFileDirectUrl(UseSiteHomeDirectory(site, file.Path), expiresOn);

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

	public async Task<System.IO.Stream> GetFileContents(Site site, File file)
	{
		FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, file.Provider);
		return await provider.GetFileContents(UseSiteHomeDirectory(site, file.Path));
	}

	public async Task DeleteFile(Site site, File file)
	{
		FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, file.Provider);
		//Folder parentFolder = await provider.GetFolder(UseSiteHomeDirectory(site, file.Parent.Path));
		Folder parentFolder = await this.GetFolder(site, file.Provider, file.Parent?.Path);
		await provider.DeleteFile(UseSiteHomeDirectory(site, file.Path));

		using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
		{
			await dataProvider.DeleteFile(file);
		}

		this.CacheManager.FileCache().Remove(file.Id);
		this.CacheManager.FilePathCache().Remove(FileSystemCachePath(site, file.Provider, file.Path));

		if (parentFolder != null)
		{
			this.CacheManager.FolderCache().Remove(parentFolder.Id);
			this.CacheManager.FolderPathCache().Remove(FileSystemCachePath(site, parentFolder.Provider, parentFolder.Path));
		}
	}

	public async Task RenameFile(Site site, File file, string newName)
	{
		FileSystemProvider provider = this.FileSystemProviderFactory.Get(site, file.Provider);
		Folder parentFolder = await this.GetFolder(site, file.Provider, file.Parent.Path);
		string message = "";

		if (!IsValidFileName(parentFolder, newName, ref message))
		{
			throw new InvalidOperationException(message);
		}

		if (!System.IO.Path.GetExtension(file.Name).Equals(System.IO.Path.GetExtension(newName), StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Changing the file extension is not allowed.");
		}

		File newfile = RemoveSiteHomeDirectory(site, await provider.RenameFile(UseSiteHomeDirectory(site, file.Path), newName));

		using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
		{
			file.Path = newfile.Path;
			await dataProvider.SaveFile(site, file);
		}

		this.CacheManager.FileCache().Remove(file.Id);
		this.CacheManager.FilePathCache().Remove(FileSystemCachePath(site, file.Provider, file.Path));

		if (parentFolder != null)
		{
			this.CacheManager.FolderCache().Remove(parentFolder.Id);
			this.CacheManager.FolderPathCache().Remove(FileSystemCachePath(site, parentFolder.Provider, parentFolder.Path));
		}
	}

	public async Task<Folder> SaveFolder(Site site, Folder folder)
	{
		using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
		{
			folder = await dataProvider.SaveFolder(site, RemoveSiteHomeDirectory(site, folder));

			this.CacheManager.FolderCache().Remove(folder.Id);
			this.CacheManager.FolderPathCache().Remove(FileSystemCachePath(site, folder.Provider, folder.Path));

			return folder;
		}
	}

	public async Task<File> SaveFile(Site site, string providerName, string parentPath, string newFileName, System.IO.Stream content, Boolean overwrite)
	{
		FileSystemProvider fileSystemProvider = this.FileSystemProviderFactory.Get(site, providerName);

		if (content != null)
		{
			if (parentPath.Length > 1 && (parentPath.EndsWith('/') || parentPath.EndsWith('\\')))
			{
				parentPath = parentPath[..^1];
			}

			Folder parentFolder = await fileSystemProvider.GetFolder(UseSiteHomeDirectory(site, parentPath));
			string message = "";

			if (!IsValidFileName(parentFolder, newFileName, ref message))
			{
				throw new InvalidOperationException(message);
			}

			if (content.CanSeek)
			{
				content.Position = 0;
			}
			File file = RemoveSiteHomeDirectory(site, await fileSystemProvider.SaveFile(UseSiteHomeDirectory(site, parentPath), newFileName, content, overwrite));

			await GetDatabaseProperties(site, file);

			// Expire the direct url after upload.  By generating a new url, we make browsers download image files again rather than using
			// a browser-cached copy.
			if (!String.IsNullOrEmpty(file.DirectUrl))
			{
				file.DirectUrlExpiry = DateTime.UtcNow;
			}

			// try to get the image dimensions and save them.  The GetImageDimensions extension checks that the file is
			// an image and does nothing if it is not, so we don't need to check that here. 
			await file.GetImageDimensions(site, this);

			using (IFileSystemDataProvider dataProvider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
			{
				await dataProvider.SaveFile(site, file);
			}
			this.CacheManager.FileCache().Remove(file.Id);
			this.CacheManager.FilePathCache().Remove(FileSystemCachePath(site, file.Provider, file.Path));

			return await GetFile(site, file.Id);
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
					file = RemoveSiteHomeDirectory(site, await this.GetFile(site, file.Id));
				}

				// if the file system provider does not set the parent, read it
				if (file.Parent == null)
				{
					file.Parent = await this.GetFolder(site, file.Provider ?? this.ListProviders().FirstOrDefault()?.Key, "");
				}

				if (file.Parent != null)
				{
					RemoveSiteHomeDirectory(site, file.Parent);
					file.Parent.Permissions = await this.ListPermissions(file.Parent);
				}
			}
		}
		catch (System.IO.FileNotFoundException)
		{
			// in case file has been deleted
		}

		return file;
	}

	/// <summary>
	/// Add the site's home directory to the start of the specified path, if a home directory has been set, ensuring that the returned path does
	/// not end with trailing directory separator characters. 
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private string UseSiteHomeDirectory(Site site, string path)
	{
		if (String.IsNullOrEmpty(site.HomeDirectory))
		{
			return path;
		}
		else
		{
			if (String.IsNullOrEmpty(path))
			{
				return site.HomeDirectory;
			}
			else
			{
				return $"{site.HomeDirectory}{(!path.StartsWith(System.IO.Path.AltDirectorySeparatorChar) ? System.IO.Path.AltDirectorySeparatorChar : "")}{path}";
			}
		}
	}

	private T RemoveSiteHomeDirectory<T>(Site site, T item)
		where T : FileSystemItem
	{
		if (item == null) return null;
		item.Path = RemoveSiteHomeDirectory(site, item.Path);
		if (item.Parent != null)
		{
			item.Parent.Path = RemoveSiteHomeDirectory(site, item.Parent.Path);
		}
		return item;
	}

	private string RemoveSiteHomeDirectory(Site site, string path)
	{
		if (String.IsNullOrEmpty(path))
		{
			return path;
		}
		else if (String.IsNullOrEmpty(site.HomeDirectory))
		{
			return path.Trim(DirectorySeparators);
		}
		else
		{
			if (path.StartsWith(site.HomeDirectory, StringComparison.OrdinalIgnoreCase))
			{
				return path.Substring(site.HomeDirectory.Length).Trim(DirectorySeparators);
			}
			else
			{
				return path.Trim(DirectorySeparators);
			}
		}
	}


	/// <summary>
	/// Copy permissions from the specified <paramref name="folder"/> to its descendants.
	/// </summary>
	/// <param name="site"></param>
	/// <param name="folder"></param>
	/// <param name="user"></param>
	/// <param name="operation">
	/// If <paramref name="operation"/> is <see cref="IFileSystemManager.CopyPermissionOperation.Replace"/> overwrite all permissions of descendant folders.  
	/// if <paramref name="operation"/> is <see cref="IFileSystemManager.CopyPermissionOperation.Merge"/>, merge the descendant folder permissions with the specified 
	/// <paramref name="folder"/> permissions.		
	/// </param>
	/// <returns></returns>
	public async Task<Boolean> CopyPermissionsToDescendants(Site site, Folder folder, ClaimsPrincipal user, CopyPermissionOperation operation)
	{
		Folder parentFolder = await this.ListFolder(site, folder.Id, "");

		List<Permission> permissions = await this.ListPermissions(folder);

		if (parentFolder.Folders.Any())
		{
			foreach (Folder childFolder in parentFolder.Folders)
			{
				await CopyPermissionsToDescendants(site, childFolder, permissions, operation);
			}
			return true;
		}
		else
		{
			return false;
		}
	}

	private async Task CopyPermissionsToDescendants(Site site, Folder folder, List<Permission> parentPermissions, CopyPermissionOperation operation)
	{
		List<Permission> newPermissions = new();

		List<Permission> originalPermissions = await this.PermissionsManager.ListPermissions(folder.Id, Folder.URN);

		switch (operation)
		{
			case CopyPermissionOperation.Replace:
				// this is the default behavior
				newPermissions = parentPermissions
					.Select(parentPermission => new Permission()
					{
						AllowAccess = parentPermission.AllowAccess,
						PermissionType = parentPermission.PermissionType,
						RelatedId = parentPermission.RelatedId,
						Role = parentPermission.Role
					}).ToList();
				break;

			case CopyPermissionOperation.Merge:
				// merge original permissions with new permissions
				newPermissions = parentPermissions
					.Select(parentPermission => new Permission()
					{
						AllowAccess = parentPermission.AllowAccess,
						PermissionType = parentPermission.PermissionType,
						RelatedId = parentPermission.RelatedId,
						Role = parentPermission.Role
					}).ToList();

				// Include original permissions where the role is not in parentPermissions
				newPermissions.AddRange(originalPermissions.Where
				(
					originalPermission => !newPermissions.Where
					(
						newPermission =>
						(
							newPermission.RelatedId == originalPermission.RelatedId &&
							newPermission.Role.Id == originalPermission.Role.Id &&
							newPermission.PermissionType.Scope.Equals(originalPermission.PermissionType.Scope, StringComparison.OrdinalIgnoreCase) &&
							newPermission.AllowAccess == originalPermission.AllowAccess
						)
					).Any()
				));

				break;
		}

		await this.PermissionsManager.SavePermissions(folder.Id, newPermissions, originalPermissions);

		this.CacheManager.FolderCache().Remove(folder.Id);
		this.CacheManager.FolderPathCache().Remove(FileSystemCachePath(site, folder.Provider, folder.Path));
    
    // Query the folder for propagation
    Folder parentFolder = await this.ListFolder(site, folder.Id, "");

    if (parentFolder.Folders.Any())
    {
      foreach (Folder childFolder in parentFolder.Folders)
      {
        await CopyPermissionsToDescendants(site, childFolder, parentPermissions, operation);
      }
    }
  }

  /// <summary>
  /// Return a list of all <see cref="File"/>s for the site which match the specified search term.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="searchTerm"></param>
  /// <param name="userRoleNames"></param>
  /// <param name="pagingSettings"></param>
  /// <returns></returns>
  public async Task<Nucleus.Abstractions.Models.Paging.PagedResult<File>> Search(Site site, string searchTerm, IEnumerable<Role> userRoles, Nucleus.Abstractions.Models.Paging.PagingSettings pagingSettings)
  {    
    using (IFileSystemDataProvider provider = this.DataProviderFactory.CreateProvider<IFileSystemDataProvider>())
    {
      List<File> files = await provider.SearchFiles(site, searchTerm);
      List<File> viewableFiles = [];

      // we have to do permissions checking and paging here, because the database does not store parent/child (Folder/File) relationships,
      // and permissions are on the Folder, not the file.
      foreach (File file in files)
      {
        if (await CanViewFile(site, file, userRoles))
        {
          viewableFiles.Add(file);
        }
      }

      return new(pagingSettings)
      {
        TotalCount = viewableFiles.Count,
        Items = viewableFiles
          .Skip(pagingSettings.FirstRowIndex)
          .Take(pagingSettings.PageSize)
          .ToList()
      };
    }
  }

  private async Task<Boolean> CanViewFile(Site site, File file, IEnumerable<Role> userRoles)
  {
    try
    {
      File fullFile = await this.GetFile(site, file.Id);

      if (userRoles == null || fullFile.Parent.IncludeInSearch)
      {
        if (userRoles == null || !userRoles.Any()) return true;

        foreach (Permission permission in fullFile.Parent.Permissions)
        {
          if (permission.IsFolderViewPermission())
          {

            if (permission.AllowAccess && permission.Role != null)
            {
              if (userRoles.Any(role => role.Id == permission.Role.Id))
              {
                return true;
              }
            }
          }
        }
      }
    }
    catch (System.IO.FileNotFoundException)
    {
      return false;
    }

    return false;
  }  
}
