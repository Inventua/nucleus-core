using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Hosting;
using Nucleus.Core.Logging;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Nucleus.Core.Plugins
{
	/// <summary>
	/// Provides methods used to load Nucleus Core plugins into dependency injection.
	/// </summary>
	public static	class PluginExtensions
	{
		public static IMvcBuilder AddPlugins(this IMvcBuilder builder, string contentRootPath)
		{
			builder.AddExternalControllers();

			builder.AddCompiledRazorViews();
			builder.AddScheduledTasks();

			builder.Services.Configure<RazorViewEngineOptions>(options =>
			{
				options.ViewLocationExpanders.Add(new ModuleViewLocationExpander());				
			});

			return builder;
		}

		/// <summary>
		/// Iterate through assemblies in /bin and /modules/**, add assemblies which contain Controller classes to Part Manager so that DI/MVC 
		/// can use them.  The call to GetControllerAssemblies checks for classes which implement Microsoft.AspNetCore.Mvc.Controller.	 
		/// </summary>
		/// <param name="builder">IMvcBuilder instance used to configure services.</param>
		/// <returns>IMvcBuilder instance.</returns>
		public static IMvcBuilder AddExternalControllers(this IMvcBuilder builder)
		{
			
			foreach (Assembly assembly in AssemblyLoader.GetAssembliesImplementing<Microsoft.AspNetCore.Mvc.Controller>(builder.Logger()))
			{
				builder.Logger().LogInformation($"Adding controllers from {assembly.FullName}.");

				Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart part = new(assembly);

				// only add assembly to application parts if it is not already present (adding twice throws a AmbiguousMatchException exception)
				if (!ApplicationPartContains(builder, part))
				{
					builder.PartManager.ApplicationParts.Add(part);
				}
			}

			return builder;
		}

		/// <summary>
		/// Add types in assemblies from /bin and in extension folders which implement <see cref="Nucleus.Abstractions.IScheduledTask"/> 
		/// to dependency injection.
		/// </summary>
		/// <param name="builder"></param>
		/// <returns></returns>
		/// <remarks>
		/// Scheduled tasks are added as Transient so that a new instance is created each time the task is run.  This means
		/// that developers of scheduled tasks don't need to worry about making their implementations stateless.
		/// </remarks>
		public static IMvcBuilder AddScheduledTasks(this IMvcBuilder builder)
		{
			foreach (Type type in AssemblyLoader.GetTypes<Nucleus.Abstractions.IScheduledTask>())
			{
				builder.Logger().LogInformation($"Adding Scheduled Task type {type.FullName}");
				builder.Services.AddTransient(type);
			}

			return builder;
		}

		/// <summary>
		/// Check whether an application part is already present in the Part Manager, as adding the same Controller assembly twice causes 
		/// a runtime AmbiguousMatchException.
		/// </summary>
		/// <param name="builder">IMvcBuilder instance used to configure services.</param>
		/// <param name="part">Part to search for.</param>
		/// <returns></returns>
		private static Boolean ApplicationPartContains(this IMvcBuilder builder, Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart part)
		{
			foreach (var existingPart in builder.PartManager.ApplicationParts)
			{
				if (existingPart is Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart)
					if (existingPart.Name == part.Name)
						return true;
			}
			return false;
		}

		/// <summary>
		/// Loop through assemblies, look for compiled Razor assemblies, and add to Part Manager so that Mvc can use them
		/// </summary>
		/// <param name="builder">IMvcBuilder instance used to configure services.</param>
		/// <returns></returns>
		public static IMvcBuilder AddCompiledRazorViews(this IMvcBuilder builder)
		{
			foreach (string assemblyFileName in AssemblyLoader.GetAssembliesWithAttribute<Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute>())
			{
				Assembly assembly = AssemblyLoader.LoadFrom(assemblyFileName);

				foreach (Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart part in Microsoft.AspNetCore.Mvc.ApplicationParts.CompiledRazorAssemblyApplicationPartFactory.GetDefaultApplicationParts(assembly))
				{
					if (!ApplicationPartContains(builder, part))
					{
						builder.Logger().LogInformation($"Adding Compiled Razor views from {part.Name}.");						
						builder.PartManager.ApplicationParts.Add(part);
					}
				}
			}

			return builder;
		}

		/// <summary>
		/// Returns a list of assemblies which implement IHostingStartup.
		/// </summary>
		/// <param name="path">The content root path.</param>
		public static IEnumerable<string> GetHostingStartupAssemblies(ILogger logger)
		{
			return AssemblyLoader.GetAssembliesImplementing<Microsoft.AspNetCore.Hosting.IHostingStartup>(logger)
				.Select(assembly => assembly.GetName().FullName);			
		}
	}
}
