using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Data.Common;

namespace Nucleus.Core.DataProviders
{
	/// Provides create, read, update and delete functionality for the <see cref="Folder"/>, <see cref="Role"/> and <see cref="RoleGroup"/> classes.
	internal interface IFileSystemDataProvider : IDisposable//, IDataProvider<IFileSystemDataProvider>
	{
		abstract Task<Folder> GetFolder(Guid folderId);
		abstract Task<Folder> GetFolder(Site site, string provider, string path);
		abstract Task<Folder> SaveFolder(Site site, Folder folder);
		abstract Task DeleteFolder(Folder folder);

		abstract Task<File> GetFile(Guid fileId);
		abstract Task<File> GetFile(Site site, string provider, string path);
		abstract Task<File> SaveFile(Site site, File file);
		abstract Task DeleteFile(File file);


	}
}
