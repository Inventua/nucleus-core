using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Core.DataProviders
{
	/// Provides create, read, update and delete functionality for the <see cref="Folder"/>, <see cref="Role"/> and <see cref="RoleGroup"/> classes.
	internal interface IFileSystemDataProvider : IDisposable, Abstractions.IDataProvider
	{
		abstract Folder GetFolder(Guid folderId);
		abstract Folder GetFolder(Site site, string provider, string path);
		abstract Folder SaveFolder(Site site, Folder folder);
		abstract void DeleteFolder(Folder folder);

		abstract File GetFile(Guid fileId);
		abstract File GetFile(Site site, string provider, string path);
		abstract File SaveFile(Site site, File file);
		abstract void DeleteFile(File file);


	}
}
