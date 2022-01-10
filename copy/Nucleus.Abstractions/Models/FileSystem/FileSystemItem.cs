using System;
using System.Collections.Generic;

namespace Nucleus.Abstractions.Models.FileSystem
{
	/// <summary>
	/// Specifies whether the file system item is a folder or file
	/// </summary>
	public enum ItemTypes
	{
		/// <summary>
		/// Specifies that the file system item is a folder
		/// </summary>
		Folder=0,
		/// <summary>
		/// Specifies that the file system item is a file
		/// </summary>
		File = 1
	}

	/// <summary>
	/// Represents an item (file or folder) stored in a file system.
	/// </summary>
	public class FileSystemItem
	{
		/// <summary>
		/// Id of database reference for the file system item
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets whether the user has selected this item on-screen.
		/// </summary>
		public Boolean IsSelected { get; set; }

		/// <summary>
		/// The file system provider name
		/// </summary>
		public string Provider { get; set; }

		/// <summary>
		/// The path of the file system item.  Path formats are defined by the relevant FileSystemProvider
		/// </summary>
		public string Path { get; set; }

		/// <summary>
		/// The path of the file system item's parent.  Path formats are defined by the relevant FileSystemProvider.  The format of a path
		/// for a "root" item is defined by the relevant FileSystemProvider.
		/// </summary>
		public Folder Parent { get; set; }

		/// <summary>
		/// Name of the file system item.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Last modified date.
		/// </summary>
		public DateTime DateModified { get; set; }

		
	}
}
