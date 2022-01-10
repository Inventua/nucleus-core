using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.FileSystemProviders;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Represents the configured file system providers.
	/// </summary>
	public class FileSystemProviderFactoryOptions
	{
		/// <summary>
		/// Configuration file section key.
		/// </summary>
		public const string Section = "Nucleus:FileSystems";

		/// <summary>
		/// List of providers from configuration.
		/// </summary>
		public List<FileSystemProviderInfo> Providers { get; private set; } = new();

		/// <summary>
		/// 
		/// </summary>
		public List<AllowedFileType> AllowedFileTypes { get; set; }
	}
}
