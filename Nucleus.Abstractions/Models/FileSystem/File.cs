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
		/// The height of an image file, in pixels.
		/// </summary>
		public int? Height { get; set; }

		/// <summary>
		/// The width of an image file, in pixels.
		/// </summary>
		public int? Width { get; set; }

		/// <summary>
		/// A saved direct url to the file, for cloud file system providers.
		/// </summary>
		public string DirectUrl { get; set; }

		/// <summary>
		/// Date/time that the DirectUrl expires.
		/// </summary>
		public DateTime? DirectUrlExpiry { get; set; }

    /// <summary>
		/// The "raw" identifier for the file, provided by the file system provider. This is not guaranted to be a Url, it is a 
    /// value used by the underlying provider as a unique Id for the file.
		/// </summary>
		public string RawUri { get; set; }

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
		public void CopyDatabaseValuesTo(File target)
		{
			target.Id = this.Id;
			target.DirectUrl= this.DirectUrl;
			target.DirectUrlExpiry = this.DirectUrlExpiry;
			target.Width = this.Width;
			target.Height = this.Height;
		}
	}
}
