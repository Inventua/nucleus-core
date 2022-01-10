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
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Core.FileSystemProviders
{
	/// <summary>
	/// FileSystemProviderFactory creates and returns <see cref="FileSystemProvider"/> instances.
	/// </summary>
	public class FileSystemProviderFactory : IFileSystemProviderFactory
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
		/// Gets a ReadOnlyList of configured file system providers.
		/// </summary>
		public IReadOnlyList<FileSystemProviderInfo> Providers
		{
			get
			{
				return new List<FileSystemProviderInfo>(this.Options.Value.Providers);
			}
		}

		/// <summary>
		/// Retrieves a <see cref="FileSystemProvider"/> from the list of configured file system providers, specified by key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public FileSystemProvider Get(Site site, string key)
		{
			FileSystemProvider result;
			IConfigurationSection providers;
			IConfigurationSection section;
			Type providerType;

			FileSystemProviderInfo fileSystem = this.Providers.Where(provider => provider.Key == key).FirstOrDefault();
			if (fileSystem == null)
			{
				throw new ArgumentException("Provider not found.", nameof(key));
			}

			using (Nucleus.Core.Plugins.AssemblyLoader.EnterExtensionContext(fileSystem.ProviderType))
			{
				providerType = Type.GetType(fileSystem.ProviderType);
				if (providerType == null)
				{
					throw new InvalidOperationException($"Provider type {fileSystem.ProviderType} is not available.");
				}
			}

			result = this.ServiceProvider.GetService(providerType) as FileSystemProvider;
			result.Key = key;

			//providers = 
			//	this.Configuration.GetSection(FileSystemProviderFactoryOptions.Section).GetChildren()
			//		.Where(section => section.Key == nameof(FileSystemProviderFactoryOptions.Providers))
			//		.FirstOrDefault();
			
			string configKey = $"{FileSystemProviderFactoryOptions.Section}:{nameof(FileSystemProviderFactoryOptions.Providers)}";

			providers =	this.Configuration.GetSection(configKey);

			if (providers == null)
			{
				throw new InvalidOperationException($"File system provider configuration section '{configKey}' is missing.");
			}

			section = providers.GetChildren().Where(child => child.GetSection("Key").Value == key).FirstOrDefault();

			//section = this.Configuration.GetSection(FileSystemProviderFactoryOptions.Section)
			//		.GetChildren().Where(section => section.Key == nameof(FileSystemProviderFactoryOptions.Providers)).FirstOrDefault()
			//		?.GetChildren().Where(section => section.Key == key).FirstOrDefault();

			result.Configure(section, site.HomeDirectory);
			
			return result;
		}
	}
}
