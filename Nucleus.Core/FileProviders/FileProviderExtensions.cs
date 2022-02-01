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
using Nucleus.Abstractions.Models.Configuration;

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
		/// Add and configure the <see cref="MergedFileProviderMiddleware"/>.
		/// </summary>
		/// <param name="services">.NET core dependency injection services collection.</param>
		/// <param name="configuration">.NET core configuration object used to access configuration items.</param>
		public static void AddMergedFileMiddleware(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, IConfiguration configuration)
		{			
			services.Configure<MergedFileProviderOptions>(configuration.GetSection(MergedFileProviderOptions.Section));
			services.AddSingleton<MergedFileProviderMiddleware>();
		}

	}
}
