//using System;
//using System.Collections.Generic;
//using System.Runtime.Loader;
//using System.Reflection;
//using System.Linq;
//using Microsoft.Extensions.Logging;
//using Microsoft.AspNetCore.Http;
//using Nucleus.Abstractions;

//namespace Nucleus.Extensions.Plugins
//{
//	/// <summary>
//	/// The AssemblyLoader class contains methods used by OsaAppCore which are used to load OsaAppCore application (plugin) assemblies.
//	/// </summary>
//	/// <remarks>
//	/// The methods within this class are used by OsaAppCore.  OsaAppCore applications (plugins) would not typically need to use the methods
//	/// of the AssemblyLoader class.
//	/// </remarks>
//	public class AssemblyLoader
//	{
//		//public static string PluginIncludeFolder { get; set; }

//		private static SortedList<string, AssemblyLoadContext> AssemblyLoadContexts { get; } = new SortedList<string, AssemblyLoadContext>();

//		/// <summary>
//		/// Load an assembly into the appropriate AssemblyLoadContext. 
//		/// </summary>
//		/// <param name="path">The full path and filename of the assembly to load.</param>
//		/// <returns>The loaded assembly.</returns>
//		/// <remarks>
//		/// The LoadFrom method checks the path of the assembly and separates plugin assemblies into their own AssemblyLoadContext.  This
//		/// allows plugins to use their own version of referenced assemblies, if they are present in the plugin's bin folder, or use the versions
//		/// which are shipped with OsaAppCore otherwise.
//		/// </remarks>
//		public static Assembly LoadFrom(string path)
//		{
//			AssemblyLoadContext context;
//			string pluginPath;

//			if (path.LastIndexOf("/bin") >= 0)
//				pluginPath = path.Substring(0, path.LastIndexOf("/bin") + 4);
//			else
//				pluginPath = typeof(AssemblyLoader).Assembly.Location;

//			if (pluginPath == typeof(AssemblyLoader).Assembly.Location)
//			{
//				context = AssemblyLoadContext.Default;
//			}
//			else
//			{
//				if (AssemblyLoadContexts.ContainsKey(pluginPath))
//				{
//					context = AssemblyLoadContexts[pluginPath];
//				}
//				else
//				{
//					context = new PluginLoadContext(pluginPath);
//					AssemblyLoadContexts.Add(pluginPath, context);
//				}
//			}

//			try
//			{
//				// exclude anything in the bin/runtimes folder, these are not .net assemblies
//				if (System.IO.Path.GetDirectoryName(path).ToLower().Contains("\\runtimes\\") && System.IO.Path.GetDirectoryName(path).ToLower().EndsWith("\\native"))
//				{
//					return null;
//				}
//				else
//				{
//					return context.LoadFromAssemblyPath(path); }
//				}
//			catch
//			{
//				// not a .net assembly
//				return null;
//			}
//		}

//		/// <summary>
//		/// Enumerate all Assemblies (dlls) including OsaAppCore assemblies and plugin assemblies.
//		/// </summary>
//		/// <param name="path">The content root path for OsaAppCore.</param>
//		/// <returns></returns>
//		private static List<string> EnumerateAssemblies(string path)
//		{
//			List<string> results = new List<string>();

//			// get all assemblies (dlls) in /bin 
//			//foreach (string assemblyFileName in System.IO.Directory.EnumerateFiles(System.IO.Path.Combine(path, "bin"), "*.dll", System.IO.SearchOption.AllDirectories))
//			foreach (string assemblyFileName in System.IO.Directory.EnumerateFiles(System.IO.Path.GetDirectoryName(typeof(AssemblyLoader).Assembly.Location), "*.dll", System.IO.SearchOption.AllDirectories))
//			{
//				results.Add(assemblyFileName);
//			}

//			// get all module assemblies (dlls) 
//			foreach (string assemblyFileName in System.IO.Directory.EnumerateFiles(Folders.GetModulesFolder(), "*.dll", System.IO.SearchOption.AllDirectories))
//			{
//				results.Add(assemblyFileName);
//			}

//			// if an external folder has been specified to include a plugin from, add it
//			//if (!String.IsNullOrEmpty(PluginIncludeFolder))
//			//{
//			//	foreach (string assemblyFileName in System.IO.Directory.EnumerateFiles(PluginIncludeFolder, "*.dll", System.IO.SearchOption.AllDirectories))
//			//	{
//			//		results.Add(assemblyFileName);
//			//	}
//			//}

//			return results;
//		}

//		/// <summary>
//		/// Return a list of assemblies which have the assembly attribute specified by T.
//		/// </summary>
//		/// <typeparam name="T"></typeparam>
//		/// <param name="path">The content root path for OsaAppCore.</param>
//		/// <returns></returns>
//		public static IList<string> GetAssembliesWithAttribute<T>(string path)
//		{
//			SortedList<string, string> results = new SortedList<string, string>();

//			foreach (string assemblyFileName in EnumerateAssemblies(path))
//			{
//				Assembly assembly = AssemblyLoader.LoadFrom(assemblyFileName);

//				if (assembly != null)
//				{
//					CustomAttributeData attributes = assembly.CustomAttributes.FirstOrDefault(assm => typeof(T).IsAssignableFrom(assm.AttributeType));

//					if (attributes != null)
//					{
//						//Util.StartupLogger.LogDebug($"Found Razor views in {assemblyFileName}.");

//						if (!results.ContainsKey(assemblyFileName))
//							results.Add(assemblyFileName, assemblyFileName);
//					}
//				}
//			}

//			return results.Values;
//		}

//		/// <summary>
//		/// Return a list of assemblies which implement the class specified by T.
//		/// </summary>
//		/// <typeparam name="T">The type to search for.</typeparam>
//		/// <param name="path">The content root path for OsaAppCore.</param>
//		/// <returns></returns>
//		internal static IList<string> GetAssembliesImplementing<T>(string path)
//		{
//			SortedList<string, string> results = new SortedList<string, string>();
//			//Util.StartupLogger.LogTrace($"GetAssembliesImplementing: Check path {0}", path);

//			foreach (string assemblyFileName in EnumerateAssemblies(path))
//			{
//				// Load all assemblies EXCEPT Razor views (because they don't work)
//				if (!System.IO.Path.GetFileName(assemblyFileName).EndsWith("Views.dll"))
//				{
//					Assembly assembly = AssemblyLoader.LoadFrom(assemblyFileName);

//					if (assembly != null)
//					{
//						Type types = assembly.GetTypes().FirstOrDefault
//						(
//							typ => typeof(T).IsAssignableFrom(typ)
//						);

//						if (types != null)
//						{
//							if (!results.ContainsKey(assemblyFileName))
//								results.Add(assemblyFileName, assemblyFileName);
//						}
//					}
//				}
//			}

//			return results.Values;
//		}

//		/// <summary>
//		/// Return a list of types which implement the class specified by T. 
//		/// </summary>
//		/// <typeparam name="T">The type to search for.</typeparam>
//		/// <param name="path">The content root path for OsaAppCore.</param>
//		/// <returns>A list of .Net types which implement T.</returns>
//		public static IList<Type> GetTypes<T>(string path)
//		{
//			List<Type> results = new List<Type>();
//			//Util.StartupLogger.LogTrace($"GetTypes: Check path {0}", path);

//			foreach (string assemblyFileName in EnumerateAssemblies(path))
//			{
//				// Load all assemblies EXCEPT Razor views (because they don't work)
//				if (!System.IO.Path.GetFileName(assemblyFileName).EndsWith("Views.dll"))
//				{
//					Assembly assembly = AssemblyLoader.LoadFrom(assemblyFileName);

//					if (assembly != null)
//					{
//						IEnumerable<Type> types = from type in assembly.GetTypes()
//																			where typeof(T).IsAssignableFrom(type)
//																			select type;

//						results.AddRange(types);
//					}
//				}
//			}

//			return results;
//		}
//	}
//}
