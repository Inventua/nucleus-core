using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions.AmazonS3FileSystemProvider
{
	/// <summary>
	/// Custom options for the <see cref="FileSystemProvider"/>.
	/// </summary>
	public class FileSystemProviderOptions 
	{
		public string AccessKey { get; set; }
		public string Secret { get; set; }

		public string ServiceUrl { get; set; }
	}
}
