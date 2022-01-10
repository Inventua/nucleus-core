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
	/// <remarks>
	/// The methods within this class are used by Nucleus Core.  Nucleus Core applications (plugins) would not typically need to use the methods
	/// of the PluginExtensions class.
	/// </remarks>
	public static	class PluginExtensions
	{
		public static IMvcBuilder AddPlugins(this IMvcBuilder builder, string contentRootPath)
		{
			builder.AddExternalControllers();

			// replaced by ModuleViewLocationExpander
			//builder.AddExternalViews(contentRootPath);  

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
			
			foreach (Assembly assembly in AssemblyLoader.GetAssembliesImplementing<Microsoft.AspNetCore.Mvc.Controller>())
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
		/// to dependnecy injection.
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
		/// Normalize a path by replacing the "/" character with "\"
		/// </summary>
		/// <param name="Path"></param>
		/// <param name="WebRootPath"></param>
		/// <returns></returns>
		private static string FormatPath(string Path, string WebRootPath)
		{
			return Path.Replace(WebRootPath, "", StringComparison.OrdinalIgnoreCase).Replace('\\', '/');
		}

		/// <summary>
		/// Iterate through views in /modules/**/views, add assemblies which contain Controller classes to Part Manager so that DI/MVC 
		/// can use them.  
		/// </summary>
		/// <param name="builder">IMvcBuilder instance used to configure services.</param>
		/// <param name="path">The content root path.</param>
		/// <returns>IMvcBuilder instance.</returns>
		//
		//
		// Replaced by the ModuleViewLocationExpander
		//
		//
		//public static IMvcBuilder AddExternalViews(this IMvcBuilder builder, string path)
		//{
		//	builder.Services.Configure<RazorViewEngineOptions>(options =>
		//	{
		//		// Add built-in views and pages
		//		AddViewsAndPagesPath(builder, options, "Views", path);
		//		AddViewsAndPagesPath(builder, options, "Views/Shared", path);		

		//		// Add views and pages from extensions
		//		foreach (string folder in System.IO.Directory.GetDirectories(System.IO.Path.Combine(path, Nucleus.Abstractions.Folders.EXTENSIONS_FOLDER)))
		//		{
		//			string pagesPath = System.IO.Path.Combine(folder, "Pages");
		//			string viewsPath = System.IO.Path.Combine(folder, "Views");
		//			string sharedViewsPath = System.IO.Path.Combine(viewsPath, "Shared");

		//			AddViewsAndPagesPath(builder, options, pagesPath, path);
		//			AddViewsAndPagesPath(builder, options, viewsPath, path);
		//			AddViewsAndPagesPath(builder, options, sharedViewsPath, path);
		//		}
		//	});

		//	return builder;
		//}

		//private static void AddViewsAndPagesPath(IMvcBuilder builder, RazorViewEngineOptions options, string Path, string WebRootPath)
		//{
		//	if (System.IO.Directory.Exists(Path))
		//	{
		//		string path = FormatPath(System.IO.Path.Combine(Path, $"{{0}}{RazorViewEngine.ViewExtension}"), WebRootPath);

		//		builder.Logger().LogInformation($"Adding Views and Razor Pages pattern: {path} [{{0}}: Action, {{1}}: Controller]");
		//		options.ViewLocationFormats.Add(path);
		//		options.PageViewLocationFormats.Add(path);

		//		options.AreaViewLocationFormats.Add(path);
		//		options.AreaPageViewLocationFormats.Add(path);
		//	}
		//}

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
		public static IEnumerable<string> GetHostingStartupAssemblies()
		{
			return AssemblyLoader.GetAssembliesImplementing<Microsoft.AspNetCore.Hosting.IHostingStartup>().Select(assembly => assembly.GetName().FullName);			
		}
	}
}
