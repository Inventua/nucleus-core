using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.FileSystemProviders
{
	/// <summary>
	/// Represents a single configured file system provider.
	/// </summary>
	public class FileSystemProviderInfo
	{
		/// <summary>
		/// File system provider unique identifier.
		/// </summary>
		public string Key { get; private set; }

		/// <summary>
		/// File system provider "Friendly" name.
		/// </summary>
		/// <remarks>
		/// This value is displayed in the user interface.
		/// </remarks>
		public string Name { get; private set; }

		/// <summary>
		/// Assembly-qualified type name of the <see cref="FileSystemProvider"/>.
		/// </summary>
		public string ProviderType { get; private set; }

		//public IConfigurationSection ConfigurationSection { get; private set; }

	}
}
