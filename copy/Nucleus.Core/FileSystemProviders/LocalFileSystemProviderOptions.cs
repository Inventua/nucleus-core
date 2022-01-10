using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Core.FileSystemProviders
{
	/// <summary>
	/// Custom options for the <see cref="LocalFileSystemProvider"/>.
	/// </summary>
	public class LocalFileSystemProviderOptions
	{
		public string RootFolder { get; set; }
		public string[] AllowedTypes { get; set; }
	}
}
