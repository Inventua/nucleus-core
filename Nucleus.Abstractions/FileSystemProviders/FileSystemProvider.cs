using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;
using Microsoft.Extensions.Configuration;

namespace Nucleus.Abstractions.FileSystemProviders
{
	/// <summary>
	/// Abstract class implemented by all file system providers.
	/// </summary>
	abstract public class FileSystemProvider 
	{
		/// <summary>
		/// File system provider key (from configuration)
		/// </summary>
		public string Key { get; set; }

		/// <summary>
		/// Sets the configuration for this instance.
		/// </summary>
		/// <param name="configSection"></param>
		/// <param name="HomeDirectory"></param>
		public abstract void Configure(IConfigurationSection configSection, string HomeDirectory);

		/// <summary>
		/// Create a new folder.
		/// </summary>
		/// <param name="parentPath"></param>
		/// <param name="newFolder"></param>
		/// <returns></returns>
		public abstract Folder CreateFolder(string parentPath, string newFolder);

		/// <summary>
		/// Delete an existing folder.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="recursive"></param>
		public abstract void DeleteFolder(string path, Boolean recursive);

		/// <summary>
		/// Rename an existing folder.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="newName"></param>
		/// <returns></returns>
		public abstract Folder RenameFolder(string path, string newName);

		/// <summary>
		/// List the contents of a folder.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public abstract Folder ListFolder(string path);

		/// <summary>
		/// List the contents of a folder, filtered by the regular expression specified in <paramref name="pattern"/>.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="pattern"></param>
		/// <returns></returns>
		public abstract Folder ListFolder(string path, string pattern);

		/// <summary>
		/// Get an existing folder.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public abstract Folder GetFolder(string path);

		/// <summary>
		///  Get an existing file.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public abstract File GetFile(string path);

		/// <summary>
		///  Get an direct access Url for the file.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		/// <remarks>
		/// This method can return null to indicate that the file system provider cannot provide a direct Url to the file.
		/// </remarks>
		public abstract System.Uri GetFileDirectUrl(string path);

		/// <summary>
		/// Retrieve the contents of an existing file and return as a stream.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public abstract System.IO.Stream GetFileContents(string path);

		/// <summary>
		/// Delete an existing file.
		/// </summary>
		/// <param name="path"></param>
		public abstract void DeleteFile(string path);

		/// <summary>
		/// Rename an existing file.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="newName"></param>
		/// <returns></returns>
		public abstract File RenameFile(string path, string newName);

		/// <summary>
		/// Save a file.
		/// </summary>
		/// <param name="parentPath"></param>
		/// <param name="newFileName"></param>
		/// <param name="content"></param>
		/// <param name="overwrite"></param>
		/// <returns></returns>
		public abstract Task<File> SaveFile(string parentPath, string newFileName, System.IO.Stream content, Boolean overwrite);
		
	}
}
