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
	}
}
