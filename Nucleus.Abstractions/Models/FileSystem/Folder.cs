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

		/// <summary>
		/// Specifies whether to include the files within the folder in the search index.
		/// </summary>
		public Boolean IncludeInSearch { get; set; } = true;


		/// <summary>
		/// Specifies a list of name validation rules (regular expressions) and error messages for folders within this folder.
		/// </summary>
		public IEnumerable<FileSystemValidationRule> FolderValidationRules { get; set; }

		/// <summary>
		/// Specifies a list of name validation rules (regular expressions) and error messages for files within this folder.
		/// </summary>
		public IEnumerable<FileSystemValidationRule> FileValidationRules { get; set; }

		/// <summary>
		/// Data for folder items is retrieved from the database, and from the file system.  This function "merges" a Folder
		/// object which has been retrieved from the file system with one from the database by copying the fields that are only
		/// present in the database (that is, can't be retrieved from the file system).
		/// </summary>
		/// <param name="target"></param>
		public void CopyDatabaseValuesTo(Folder target)
		{
			target.Id = this.Id;
			target.IncludeInSearch = this.IncludeInSearch;
			target.Permissions = this.Permissions;
		}
	}
}
