using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System.Collections.ObjectModel;
using Nucleus.Abstractions.FileSystemProviders;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Defines the interface for the File System manager.
	/// </summary>
	public interface IFileSystemManager
	{
		/// <summary>
		/// Retrieve an existing <see cref="Folder"/> from the database.
		/// </summary>
		/// <returns></returns>
		public Task<Folder> GetFolder(Site site, string providerName, string path);

		/// <summary>
		/// Retrieve an existing <see cref="Folder"/> from the database.
		/// </summary>
		/// <returns></returns>
		public Task<Folder> GetFolder(Site site, Guid id);

		/// <summary>
		/// Retrieve an existing <see cref="File"/> from the database.
		/// </summary>
		/// <returns></returns>
		public Task<File> GetFile(Site site, Guid id);

		/// <summary>
		/// Retrieve an existing <see cref="File"/> from the database, or create a new record if none exists.
		/// </summary>
		/// <returns></returns>
		public Task<File> GetFile(Site site, string providerName, string path);

		/// <summary>
		/// Get a direct url for the file.
		/// </summary>
		/// <param name="file"></param>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// Returns null to indicate that a direct url could not be returned for the specified file.  Check the file 
		/// Capabilities.CanDirectLink property to determine whether the file system provider is capable of 
		/// providing direct links before calling this method.
		/// </remarks>
		public System.Uri GetFileDirectUrl(Site site, File file);

		/// <summary>
		/// Create/add default permissions to the specified <see cref="Folder"/> for the specified <see cref="Role"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="folder"></param>
		/// <param name="role"></param>
		/// <remarks>
		/// The new <see cref="Permission"/>s are not saved unless you call <see cref="SaveFolderPermissions(Site,Folder)"/>.
		/// </remarks>
		public Task CreatePermissions(Site site, Folder folder, Role role);

		/// <summary>
		/// Save <see cref="Folder"/> data to the database.
		/// </summary>
		/// <param name="folder"></param>
		/// <param name="site"></param>
		public Task<Folder> SaveFolder(Site site, Folder folder);

		/// <summary>
		/// Save permissions for the specified <see cref="Folder"/>.
		/// </summary>
		/// <param name="folder"></param>
		/// <param name="site"></param>
		/// <remarks>
		/// A side-effect of saving folder permissions is that a database record is created for the folder, if one does not already exist.
		/// </remarks>
		public Task SaveFolderPermissions(Site site, Folder folder);

		/// <summary>
		/// List all permissions for the folder specified by folderId.
		/// </summary>
		/// <param name="folder"></param>
		/// <returns></returns>
		public Task<List<Permission>> ListPermissions(Folder folder);

		/// <summary>
		/// Return a list of available permission types, sorted by SortOrder
		/// </summary>
		/// <returns></returns>
		public Task<List<PermissionType>> ListFolderPermissionTypes();

		/// <summary>
		/// Create a new folder
		/// </summary>
		/// <param name="site"></param>
		/// <param name="providerName"></param>
		/// <param name="parentPath"></param>
		/// <param name="newFolder"></param>
		/// <returns></returns>
		public Task<Folder> CreateFolder(Site site, string providerName, string parentPath, string newFolder);

		/// <summary>
		/// Delete an existing folder
		/// </summary>
		/// <param name="site"></param>
		/// <param name="folder"></param>
		/// <param name="recursive"></param>
		public Task DeleteFolder(Site site, Folder folder, Boolean recursive);

		/// <summary>
		/// Rename an existing folder
		/// </summary>
		/// <param name="site"></param>
		/// <param name="folder"></param>
		/// <param name="newName"></param>
		/// <returns></returns>
		public Task RenameFolder(Site site, Folder folder, string newName);

		/// <summary>
		/// List all available file system providers.
		/// </summary>
		/// <returns></returns>
		public IReadOnlyList<FileSystemProviderInfo> ListProviders();

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
		public Task<Folder> ListFolder(Site site, Guid id, string pattern);

		/// <summary>
		/// Retrive the contents of the specified file and return as a stream.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="file"></param>
		/// <returns></returns>
		public System.IO.Stream GetFileContents(Site site, File file);

		/// <summary>
		/// Delete the specified file.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="file"></param>
		public Task DeleteFile(Site site, File file);

		/// <summary>
		/// Rename the specified file.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="file"></param>
		/// <param name="newName"></param>
		/// <remarks>
		/// File system providers must ensure that the file extension cannot be changed, to prevent users from
		/// uploading an unauthorized file extension renamed to another type and then renaming it.  This check
		/// is done in the FileSystemManager, but should also be done in the RenameFile implementation.
		/// </remarks>
		public Task RenameFile(Site site, File file, string newName);

		/// <summary>
		/// Save a file.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="providerName"></param>
		/// <param name="parentPath"></param>
		/// <param name="newFileName"></param>
		/// <param name="content"></param>
		/// <param name="overwrite"></param>
		/// <returns></returns>
		public Task<File> SaveFile(Site site, string providerName, string parentPath, string newFileName, System.IO.Stream content, Boolean overwrite);

		/// <summary>
		/// Refresh the properties of a file.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="file"></param>
		/// <returns></returns>
		/// <remarks>
		/// Use this method to populate the properties of a file after model binding returns a file object with just the Id property populated.
		/// </remarks>
		public Task<File> RefreshProperties(Site site, File file);
	}
}
