using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Extensions.AzureBlobStorageFileSystemProvider
{
	internal class FileSystemCapabilities
	{

		internal static FileSystemItemCapabilities Root = new FileSystemItemCapabilities()
		{
			CanHaveFolders = true,
			CanStoreFiles = false,
			CanRename = false,
			CanDelete = false
		};


		internal static FileSystemItemCapabilities Container = new FileSystemItemCapabilities()
		{
			CanStoreFiles = true,
			CanRename = false,
			CanDelete = true
		};

		internal static FileSystemItemCapabilities Folder = new FileSystemItemCapabilities()
		{
			CanStoreFiles = true,
			CanRename = false,
			CanDelete = true
		};

		internal static FileSystemItemCapabilities File = new FileSystemItemCapabilities()
		{
			CanStoreFiles = false,
			CanRename = false,
			CanDelete = true,
			CanDirectLink = true
		};
	}
}
