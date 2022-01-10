using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Nucleus.Abstractions.Models;

namespace Nucleus.Core.FileSystemProviders
{
	/// <summary>
	/// FileSystemProviderFactory creates and returns <see cref="FileSystemProvider"/> instances.
	/// </summary>
	public class FileSystemProviderFactory
	{
		private IOptions<FileSystemProviderFactoryOptions> Options { get; }
		private IServiceProvider ServiceProvider { get; }
		private IConfiguration Configuration { get; }
		
		public FileSystemProviderFactory(IOptions<FileSystemProviderFactoryOptions> options, IServiceProvider serviceProvider, IConfiguration configuration)
		{
			this.Options = options;
			this.ServiceProvider = serviceProvider;
			this.Configuration = configuration;
		}

		/// <summary>
		/// Gets a ReadOnlyDictionary of configured file system providers.
		/// </summary>
		public ReadOnlyDictionary<string, FileSystemProviderInfo> Providers
		{
			get
			{
				return new ReadOnlyDictionary<string, FileSystemProviderInfo>(this.Options.Value.Providers);
			}
		}

		/// <summary>
		/// Retrieves a <see cref="FileSystemProvider"/> from the list of configured file system providers, specified by key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		internal FileSystemProvider Get(Site site, string key)
		{
			FileSystemProvider result;
			IConfigurationSection section;
			FileSystemProviderInfo fileSystem;
			
			if (!this.Providers.TryGetValue(key, out fileSystem))
			{
				throw new ArgumentException(nameof(key));
			}

			result = this.ServiceProvider.GetService(Type.GetType(fileSystem.ProviderType)) as FileSystemProvider;
			result.Key = key;

			section = this.Configuration.GetSection(FileSystemProviderFactoryOptions.Section)
					.GetChildren().Where(section => section.Key == nameof(FileSystemProviderFactoryOptions.Providers)).FirstOrDefault()
					?.GetChildren().Where(section => section.Key == key).FirstOrDefault();

			result.Configure(section, site.HomeDirectory);
			
			return result;
		}
	}
}
