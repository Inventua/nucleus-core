using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
		public List<Folder> Folders { get; private set; } = new();

		/// <summary>
		/// Gets files stored in this folder.
		/// </summary>
		public List<File> Files { get; private set; } = new();

		/// <summary>
		/// List of folder permissions.
		/// </summary>
		public List<Permission> Permissions { get; set; } = new();

		/// <summary>
		/// Specifies whether to include the files within the folder in the search index.
		/// </summary>
		public Boolean IncludeInSearch { get; set; } = false;


		/// <summary>
		/// Specifies a list of name validation rules (regular expressions) and error messages for folders within this folder.
		/// </summary>
		public IEnumerable<FileSystemValidationRule> FolderValidationRules { get; set; }

		/// <summary>
		/// Specifies a list of name validation rules (regular expressions) and error messages for files within this folder.
		/// </summary>
		public IEnumerable<FileSystemValidationRule> FileValidationRules { get; set; }

		/// <summary>
		/// Sort this folder's sub-folders.
		/// </summary>
		public void SortFolders<TProperty>(Expression<Func<Folder, TProperty>> expression, Boolean descending)
		{
			if (descending)
			{
				this.Folders = this.Folders.OrderByDescending(expression.Compile()).ToList();
			}
			else
			{
				this.Folders = this.Folders.OrderBy(expression.Compile()).ToList();
			}
		}

		/// <summary>
		/// Sort this folder's sub-folders.
		/// </summary>
		public void SortFiles<TProperty>(Expression<Func<File, TProperty>> expression, Boolean descending)
		{
			var memberExpression = expression.Body as MemberExpression;
			if (memberExpression == null)
			{
				throw new ArgumentException("Expression must be a member expression.");
			}

			if (memberExpression.Member.Name == nameof(File.Name))
			{
				// special handling for sort by name, split into filename/extension
				if (descending)
				{
					this.Files = this.Files.OrderByDescending(file=>System.IO.Path.GetFileNameWithoutExtension(file.Name))
						.ThenByDescending(file => System.IO.Path.GetExtension(file.Name))
						.ToList();
				}
				else
				{
					this.Files = this.Files.OrderBy(file => System.IO.Path.GetFileNameWithoutExtension(file.Name))
						.ThenBy(file => System.IO.Path.GetExtension(file.Name))
						.ToList();
				}
			}
			else
			{
				if (descending)
				{
					this.Files = this.Files.OrderByDescending(expression.Compile()).ToList();
				}
				else
				{
					this.Files = this.Files.OrderBy(expression.Compile()).ToList();
				}
			}
		}

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
