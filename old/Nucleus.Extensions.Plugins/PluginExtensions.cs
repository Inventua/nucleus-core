//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using Microsoft.Extensions.Options;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using System.Runtime.Loader;
//using Microsoft.AspNetCore.Mvc.Razor;
//using Microsoft.AspNetCore.Mvc;
//using System.Diagnostics;
//using System.Text.Encodings.Web;
//using Microsoft.AspNetCore.Mvc.ViewEngines;

//namespace Nucleus.Extensions.Plugins
//{
//	/// <summary>
//	/// Provides methods used to load OsaAppCore plugins.
//	/// </summary>
//	/// <remarks>
//	/// The methods within this class are used by OsaAppCore.  OsaAppCore applications (plugins) would not typically need to use the methods
//	/// of the PluginExtensions class.
//	/// </remarks>
//	public static	class PluginExtensions
//	{

//		/// <summary>
//		/// Iterate through assemblies in /bin and /modules/**/bin, add assemblies which contain Controller classes to Part Manager so that DI/MVC 
//		/// can use them.  The call to GetControllerAssemblies checks for classes which implement Microsoft.AspNetCore.Mvc.Controller.	 
//		/// </summary>
//		/// <param name="builder">IMvcBuilder instance used to configure services.</param>
//		/// <param name="path">The content root path for OsaAppCore.</param>
//		/// <returns>IMvcBuilder instance.</returns>
//		public static IMvcBuilder AddExternalControllers(this IMvcBuilder builder, string path)
//		{
//			//builder.Services.AddSingleton<PluginRouteTransformer>();

//			foreach (string assemblyFileName in AssemblyLoader.GetAssembliesImplementing<Microsoft.AspNetCore.Mvc.Controller>(path))
//			{
//				//Assembly assembly = System.Reflection.Assembly.LoadFile(assemblyFileName);
//				Assembly assembly = AssemblyLoader.LoadFrom(assemblyFileName);
//				Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart part = new Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart(assembly);
				
//				// only add assembly to application parts if it is not already present
//				if (!ApplicationPartContains(builder, part))
//				{
//					//Util.StartupLogger.LogInformation($"Adding controllers from {assembly.FullName}.");
//					builder.PartManager.ApplicationParts.Add(part);
//				}
//			}

//			return builder;
//		}

//		private static string FormatPath(string Path, string WebRootPath)
//		{
//			return Path.Replace(WebRootPath, "", StringComparison.OrdinalIgnoreCase).Replace('\\', '/');
//		}

//		/// <summary>
//		/// Iterate through views in /modules/**/views, add assemblies which contain Controller classes to Part Manager so that DI/MVC 
//		/// can use them.  
//		/// </summary>
//		/// <param name="builder">IMvcBuilder instance used to configure services.</param>
//		/// <param name="path">The content root path.</param>
//		/// <returns>IMvcBuilder instance.</returns>
//		public static IMvcBuilder AddExternalViews(this IMvcBuilder builder, string path)
//		{
//			builder.Services.Configure<RazorViewEngineOptions>(options =>
//			{
//				AddPath(options, "Views", path);
//				AddPath(options, "Views/Shared", path);		

//				foreach (string folder in System.IO.Directory.GetDirectories(System.IO.Path.Combine(path, Nucleus.Abstractions.Folders.EXTENSIONS_FOLDER)))
//				{
//					string pagesPath = System.IO.Path.Combine(folder, "Pages");
//					string viewsPath = System.IO.Path.Combine(folder, "Views");
//					string sharedViewsPath = System.IO.Path.Combine(viewsPath, "Shared");

//					AddPath(options, pagesPath, path);
//					AddPath(options, viewsPath, path);
//					AddPath(options, sharedViewsPath, path);
//				}
//			});

//			return builder;
//		}

//		private static void AddPath(RazorViewEngineOptions options, string Path, string WebRootPath)
//		{
//			if (System.IO.Directory.Exists(Path))
//			{
//				options.ViewLocationFormats.Add(FormatPath(System.IO.Path.Combine(Path, $"{{0}}{RazorViewEngine.ViewExtension}"), WebRootPath));
//				options.PageViewLocationFormats.Add(FormatPath(System.IO.Path.Combine(Path, $"{{0}}{RazorViewEngine.ViewExtension}"), WebRootPath));
//			}
//		}

//		/// <summary>
//		/// Check whether an application part is already present in the Part Manager, as adding the same Controller assembly twice causes 
//		/// a runtime AmbiguousMatchException.
//		/// </summary>
//		/// <param name="builder">IMvcBuilder instance used to configure services.</param>
//		/// <param name="part">Part to search for.</param>
//		/// <returns></returns>
//		private static Boolean ApplicationPartContains(this IMvcBuilder builder, Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart part)
//		{
//			foreach (var existingPart in builder.PartManager.ApplicationParts)
//			{
//				if (existingPart is Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart)
//					if (existingPart.Name == part.Name)
//						return true;
//			}
//			return false;
//		}

//		/// <summary>
//		/// Loop through assemblies, look for compiled Razor assemblies, and add to Part Manager so that Mvc can use them
//		/// </summary>
//		/// <param name="builder">IMvcBuilder instance used to configure services.</param>
//		/// <param name="path">The content root path for OsaAppCore.</param>
//		/// <returns></returns>
//		public static IMvcBuilder AddRazorViews(this IMvcBuilder builder, string path)
//		{
//			foreach (string assemblyFileName in AssemblyLoader.GetAssembliesWithAttribute<Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute>(path))
//			{
//				Assembly assembly = AssemblyLoader.LoadFrom(assemblyFileName);

//				foreach (Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPart part in Microsoft.AspNetCore.Mvc.ApplicationParts.CompiledRazorAssemblyApplicationPartFactory.GetDefaultApplicationParts(assembly))
//				{
//					if (!ApplicationPartContains(builder, part))
//					{
//						//Util.StartupLogger.LogInformation($"Adding Razor views from {part.Name}.");
//						builder.PartManager.ApplicationParts.Add(part);
//					}
//				}
//			}

//			return builder;
//		}

//		/// <summary>
//		/// Iterate through assemblies and return assemblies which implement IHostingStartup.
//		/// </summary>
//		/// <param name="path">The content root path.</param>
//		/// <returns>A colon-delimited string listing the assembly full names of assemblies which implement IHostingStartup.</returns>
//		public static string GetHostingStartupAssemblies(string path)
//		{
//			string HostingStartupAssembliesKey = "";

//			foreach (string assemblyFileName in AssemblyLoader.GetAssembliesImplementing<Microsoft.AspNetCore.Hosting.IHostingStartup>(path))
//			{
//				Assembly assembly = AssemblyLoader.LoadFrom(assemblyFileName);
				
//				if (!String.IsNullOrEmpty(HostingStartupAssembliesKey))
//				{
//					HostingStartupAssembliesKey += ";";	
//				}
//				else
//				{
//					HostingStartupAssembliesKey += assembly.GetName().FullName;
//				}
//			}

//			return  HostingStartupAssembliesKey;
//		}
//	}
//}
