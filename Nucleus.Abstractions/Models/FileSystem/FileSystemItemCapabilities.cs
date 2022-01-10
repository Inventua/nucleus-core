using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.FileSystem
{
	/// <summary>
	/// Represents the operations which can be performed on a file system item.
	/// </summary>
	public class FileSystemItemCapabilities
	{
		/// <summary>
		/// Specifies whether the file system item can store files.
		/// </summary>
		public Boolean CanStoreFiles { get; set; }
		
		/// <summary>
		/// Specifies whether the file system item can be renamed.
		/// </summary>
		public Boolean CanRename { get; set; }

		/// <summary>
		/// Specifies whether the file system item can be deleted.
		/// </summary>
		public Boolean CanDelete { get; set; }

		/// <summary>
		/// Specifies whether the file system item can be accessed by a direct Url.
		/// </summary>
		public Boolean CanDirectLink { get; set; }
	}
}
