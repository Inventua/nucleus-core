using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.FileSystem
{
	/// <summary>
	/// Represents a folder stored in a file system.
	/// </summary>
	public class Folder : FileSystemItem
	{
		/// <summary>
		/// Folder entity Namespace for permissions, other scoped references
		/// </summary>
		public const string URN = "urn:nucleus:entities:folder";

		/// <summary>
		/// Gets sub-folders of this folder.
		/// </summary>
		public List<Folder> Folders { get; } = new();

		/// <summary>
		/// Gets files stored in this folder.
		/// </summary>
		public List<File> Files { get; } = new();

		/// <summary>
		/// List of folder permissions.
		/// </summary>
		public List<Permission> Permissions { get; set; } = new();

	}
}
