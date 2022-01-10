using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Models.FileSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.FileSystemProviders
{
	/// <summary>
	/// FileSystemProviderFactory creates and returns <see cref="FileSystemProvider"/> instances.
	/// </summary>
	public interface IFileSystemProviderFactory
	{
		/// <summary>
		/// Gets a ReadOnlyList of configured file system providers.
		/// </summary>
		public IReadOnlyList<FileSystemProviderInfo> Providers { get; }

		/// <summary>
		/// Retrieves a <see cref="FileSystemProvider"/> from the list of configured file system providers, specified by key.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public FileSystemProvider Get(Site site, string key);
	}
}
