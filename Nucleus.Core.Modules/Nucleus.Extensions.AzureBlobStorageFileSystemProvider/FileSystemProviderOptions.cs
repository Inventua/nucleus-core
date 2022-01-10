using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions.AzureBlobStorageFileSystemProvider
{
	/// <summary>
	/// Custom options for the <see cref="FileSystemProvider"/>.
	/// </summary>
	public class FileSystemProviderOptions 
	{
		public string ConnectionString { get; set; }
	}
}
