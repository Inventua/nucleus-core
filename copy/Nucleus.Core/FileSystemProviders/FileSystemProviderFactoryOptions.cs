using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.Core.FileSystemProviders
{
	/// <summary>
	/// Represents the configured file system providers.
	/// </summary>
	public class FileSystemProviderFactoryOptions
	{
		public const string Section = "Nucleus:FileSystems";

		public Dictionary<string, FileSystemProviderInfo> Providers { get; private set; } = new();

	}
}
