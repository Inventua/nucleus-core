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
using Nucleus.Abstractions.FileSystemProviders;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Core.FileSystemProviders
{
	/// <summary>
	/// Extension methods and supporting functions used in .net core/dependency injection startup.  
	/// </summary>
	/// <remarks>
	/// File providers are added by Nucleus Core.Host.  You would not typically need to use these function in an Nucleus Core application.
	/// </remarks>
	public static class FileSystemProviderExtensions
	{		
		/// <summary>
		/// Add and configure the <see cref="FileSystemProviderFactory"/> and <see cref="LocalFileSystemProvider"/>.
		/// </summary>
		/// <param name="services">.NET core dependency injection services collection.</param>
		/// <param name="configuration">.NET core configuration object used to access configuration items.</param>
		public static void AddFileSystemProviders(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<FileSystemProviderFactoryOptions>(configuration.GetSection(FileSystemProviderFactoryOptions.Section), binderOptions => binderOptions.BindNonPublicProperties = true);

			//FileSystemProviderFactoryOptions options =  new FileSystemProviderFactoryOptions();
			//configuration.GetSection(FileSystemProviderFactoryOptions.Section).Bind(options);						
			//services.AddSingleton<FileSystemProviderFactoryOptions>(options);
			services.AddSingleton<IFileSystemProviderFactory, FileSystemProviderFactory>();
			services.AddScoped<Nucleus.Core.FileSystemProviders.FileIntegrityCheckerMiddleware>();
			services.AddTransient<LocalFileSystemProvider>();

		}

	}
}

