using System;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Nucleus.Core.FileProviders
{
	/// <summary>
	/// Extension methods and supporting functions used in .net core/dependency injection startup.  
	/// </summary>
	/// <remarks>
	/// File providers are added by Nucleus Core.Host.  You would not typically need to use these function in an Nucleus Core application.
	/// </remarks>
	public static class FileProviderExtensions
	{
		/// <summary>
		/// Add and configure the <see cref="MergedFileProvider"/>.
		/// </summary>
		/// <param name="services">.NET core dependency injection services collection.</param>
		/// <param name="configuration">.NET core configuration object used to access configuration items.</param>
		public static void AddMergedFileProvider(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, IConfiguration configuration)
		{
			// .Configure adds the options object to the service container as a IOptions<T> and binds it to configuration
			
			// UrlHelper is required by the MergedStylesheetsTagHelper and MergedScriptsTagHelper
			services.AddSingleton<IActionContextAccessor, ActionContextAccessor>()
				.AddScoped<IUrlHelper>(
						serviceProvider => new UrlHelper(serviceProvider.GetRequiredService<IActionContextAccessor>().ActionContext)
				);

			services.AddHttpContextAccessor();

			services.Configure<MergedFileProviderOptions>(configuration.GetSection(MergedFileProviderOptions.Section));

			services.ConfigureOptions(typeof(ConfigureMergedFileProvider));
		}

		/// <summary>
		///   Add an instance of the Merged File Provider to the StaticFileOptions FileProvider property.  If a file provider is already specified, 
		///   replace the existing file provider with a CompositeFileProvider which wraps the existing provider and a new Merged File provider 
		///   instance.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		public class ConfigureMergedFileProvider : IPostConfigureOptions<StaticFileOptions>
		{
			MergedFileProviderOptions Options { get; }			
			private IWebHostEnvironment Environment { get; }
			private ILogger<MergedFileProvider> Logger { get; }
			private IHttpContextAccessor ContextAccessor { get; }

			public ConfigureMergedFileProvider(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor, IOptions<MergedFileProviderOptions> options, ILogger<MergedFileProvider> Logger)
			{				
				this.Options = options.Value;
				this.Logger = Logger;
				this.ContextAccessor = httpContextAccessor;
				this.Environment = env;
			}

			public void PostConfigure(string name, StaticFileOptions options)
			{
				MergedFileProvider objMergedFileProvider = new(this.Options, options, this.ContextAccessor, this.Logger);

				if (options.FileProvider == null)
					// No file provider set yet (null), so create a new CompositeFileProvider & assign to initialize
					options.FileProvider = new CompositeFileProvider(this.Environment.WebRootFileProvider, objMergedFileProvider);
				else
				{
					options.FileProvider = new CompositeFileProvider(this.Environment.WebRootFileProvider, options.FileProvider, objMergedFileProvider);
				}
			}
		}


		/// <summary>
		///   Automatically detect plugin assemblies (compiled Razor assemblies), and an EmbeddedFilesProvider instance for each detected assembly.
		/// </summary>
		/// <param name="services">.NET core dependency injection services collection.</param>
		public static void AddRazorEmbeddedFileProviders(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
		{
			services.ConfigureOptions(typeof(ConfigureRazorEmbeddedFileProviders));
		}

		/// <summary>
		/// Detect plugin assemblies (compiled Razor assemblies) and add an EmbeddedFilesProvider for each detected Razor assembly.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		public class ConfigureRazorEmbeddedFileProviders : IPostConfigureOptions<StaticFileOptions>
		{
			private IWebHostEnvironment Environment { get; }
			private IHttpContextAccessor Context { get; }
			private ILogger<EmbeddedFileProvider> Logger { get; }

			public ConfigureRazorEmbeddedFileProviders(IWebHostEnvironment env, IHttpContextAccessor Context, ILogger<EmbeddedFileProvider> Logger)
			{
				this.Environment = env;
				this.Context = Context;
				this.Logger = Logger;
			}

			public void PostConfigure(string name, StaticFileOptions options)
			{
				// https://stackoverflow.com/questions/51610513/can-razor-class-library-pack-static-files-js-css-etc-too
				// https://www.mikesdotnetting.com/article/330/including-static-resources-in-razor-class-libraries-in-asp-net-core
				// **Microsoft.Extensions.FileProviders.ManifestEmbeddedFileProvider objEmbeddedFilesProvider;

				EmbeddedFileProvider objEmbeddedFilesProvider;

				options.ContentTypeProvider ??= new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();

				foreach (string assemblyFileName in Plugins.AssemblyLoader.GetAssembliesWithAttribute<Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute>())
				{
					// Compiled Razor projects emit two assemblies, [Project].dll, and [Project].Views.dll.
					// The [Project].Views.dll assembly contains the RazorCompiledItemAttribute attribute and the [Project].dll assembly contains the actual embedded files.

					// The ManifestEmbeddedFileProvider requires that:
					// (a) The project file has a PropertyGroup containing
					//     <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
					// (b) The project has a reference to Microsoft.Extensions.FileProviders.Embedded

					// If you load the [Project].Views.dll assembly, the RazorCompiledItemAttribute is present, but the embedded resources manifest is not.  If you 
					// load the [Project].dll assembly, the embedded resources are there, but the RazorCompiledItemAttribute is not.  Additionally, 
					// ManifestEmbeddedFilesProvider does not work properly (cannot read the manifest) when the assembly is loaded by path - this is why we are
					// using our own implementation of EmbeddedFileProvider.  Our own implementation is also more flexible when locating static resources from
					// various locations within the assembly namespace

					DateTime modifiedDate = System.IO.File.GetCreationTime(assemblyFileName);

					Assembly assembly = Plugins.AssemblyLoader.LoadFrom(assemblyFileName.Replace(".Views", ""));
					string folder;

					if (assemblyFileName.Contains(Nucleus.Abstractions.Folders.GetExtensionsFolder()))
					{
						folder = assemblyFileName.Replace(Nucleus.Abstractions.Folders.GetExtensionsFolder(), "");
						folder = folder.Split("\\", StringSplitOptions.RemoveEmptyEntries)[0];
					}
					else
					{
						folder = "";
					}

					this.Logger.LogInformation($"Adding embedded files from {assembly.FullName}.");

					objEmbeddedFilesProvider = new EmbeddedFileProvider(this.Context, this.Logger, assembly, folder);

					if (options.FileProvider == null)
						// No file provider set yet (null), so create a new CompositeFileProvider & assign to initialize
						options.FileProvider = new CompositeFileProvider(this.Environment.WebRootFileProvider, objEmbeddedFilesProvider);
					else
					{
						// In order to add *all* of (any) assemblies which may contain embedded resources, we must create a new CompositeFileProvider
						// using the extension where we specify the existing options.FileProvider plus the new EmbeddedFilesProvider so that they are all included.
						options.FileProvider = new CompositeFileProvider(this.Environment.WebRootFileProvider, options.FileProvider, objEmbeddedFilesProvider);
					}
				}
			}
		}

		/// <summary>
		/// Add a Minified File Provider, which wraps all previously configured file providers and automatically "minifies" Javascript and CSS files.
		/// </summary>
		/// <param name="services">.NET core dependency injection services collection.</param>
		/// <param name="configuration">.NET core configuration object used to access configuration items.</param>
		public static void AddMinifiedFileProvider(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, IConfiguration configuration)
		{
			// .Configure adds the options object to the service container as a IOptions<T> and binds it to configuration
			services.Configure<MinifiedFileProviderOptions>(configuration.GetSection(MinifiedFileProviderOptions.Section));

			services.ConfigureOptions(typeof(ConfigureMinifiedFileProvider));
		}

		/// <summary>
		///  Create a minified file provider to intercept all static file requests and minify the results.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		public class ConfigureMinifiedFileProvider : IPostConfigureOptions<StaticFileOptions>
		{
			MinifiedFileProviderOptions Options { get; }
			ILogger<MinifiedFileProvider> Logger { get; }

			public ConfigureMinifiedFileProvider(IOptions<MinifiedFileProviderOptions> options, ILogger<MinifiedFileProvider> Logger)
			{
				this.Options = options.Value;
				this.Logger = Logger;
			}

			public void PostConfigure(string name, StaticFileOptions options)
			{
				// The Minified file provider works by "wrapping" all other file providers.  It performs the same tasks as the Composite file 
				// provider (as well as minification) by looping though all of its child file providers and getting the results
				options.FileProvider = new MinifiedFileProvider(this.Options, this.Logger, options.FileProvider);				
			}
		}
	}
}
