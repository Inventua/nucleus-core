using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;
using Microsoft.Extensions.Configuration;

namespace Nucleus.Core.FileSystemProviders
{
	/// <summary>
	/// Abstract class implemented by all file system providers.
	/// </summary>
	abstract public class FileSystemProvider 
	{
		public string Key { get; internal set; }

		public abstract void Configure(IConfigurationSection configSection, string HomeDirectory);

		public abstract Folder CreateFolder(string parentPath, string newFolder);
		public abstract void DeleteFolder(string path, Boolean recursive);
		public abstract Folder RenameFolder(string path, string newName);
		public abstract Folder ListFolder(string path);
		public abstract Folder ListFolder(string path, string pattern);
		public abstract Folder GetFolder(string path);
		public abstract File GetFile(string path);
		public abstract System.IO.Stream GetFileContents(string path);

		public abstract void DeleteFile(string path);
		public abstract File RenameFile(string path, string newName);

		public abstract Task<File> SaveFile(string parentPath, string newFileName, System.IO.Stream content, Boolean overwrite);
		
	}
}
