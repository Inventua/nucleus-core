using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Core.DataProviders;

/// Provides create, read, update and delete functionality for the <see cref="Folder"/>, <see cref="Role"/> and <see cref="RoleGroup"/> classes.
internal interface IFileSystemDataProvider : IDisposable
{
  abstract Task<Folder> GetFolder(Guid folderId);
  abstract Task<Folder> GetFolder(Site site, string provider, string path);
  abstract Task<Folder> SaveFolder(Site site, Folder folder);
  abstract Task DeleteFolder(Folder folder);

  abstract Task<File> GetFile(Guid fileId);
  abstract Task<File> GetFile(Site site, string provider, string path);

  abstract Task<List<File>> ListFiles(Site site, string provider, string path);
  abstract Task<List<Folder>> ListFolders(Site site, string provider, string path);

  abstract Task<File> SaveFile(Site site, File file);
  abstract Task DeleteFile(File file);

  abstract Task<List<File>> SearchFiles(Site site, string searchTerm);
}
