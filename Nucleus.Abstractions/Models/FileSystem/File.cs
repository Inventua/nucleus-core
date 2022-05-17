using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.FileSystem
{
	/// <summary>
	/// Represents a file stored in a file system.
	/// </summary>
	public class File : FileSystemItem
	{
		/// <summary>
		/// URN for the file entity, used by permission types and scopes.
		/// </summary>
		public const string URN = "urn:nucleus:entities:file";

		/// <summary>
		/// The size of the file, in bytes.
		/// </summary>
		public long Size { get; set; }
		
		/// <summary>
		/// Clear the file selection, leaving the provider and parent folder selection intact.
		/// </summary>
		public void ClearSelection()
		{
			this.Id = Guid.Empty;
			this.Path = null;
		}

		/// <summary>
		/// Data for file items is retrieved from the database, and from the file system.  This function "merges" a File
		/// object which has been retrieved from the file system with one from the database by copying the fields that are only
		/// present in the database (that is, can't be retrieved from the file system).
		/// </summary>
		/// <param name="target"></param>
		/// <remarks>
		/// At present, the only value that is exclusive to the database is 'Id', but this may change in the future, so we have
		/// centralized this functionality here so that if we add fields to the database, we only need to change code here.
		/// </remarks>
		public void CopyDatabaseValuesTo(File target)
		{
			target.Id = this.Id;
		}
	}
}
