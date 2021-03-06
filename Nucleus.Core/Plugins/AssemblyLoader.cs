using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Nucleus.Abstractions;
using Nucleus.Core;

namespace Nucleus.Core.Plugins
{
	/// <summary>
	/// The AssemblyLoader class contains methods used by Nucleus Core which are used to load Nucleus Core application (plugin/extension) 
	/// assemblies.  Assemblies within extension folders are loaded into their own AssemblyLoadContext, which ensures that extensions
	/// can include private assemblies in order to prevent included assemblies from interfering with Nucleus Core or other extensions.
	/// </summary>
	/// <remarks>
	/// The methods within this class are used by Nucleus Core.  Nucleus applications (plugins) would not typically need to use the methods
	/// of the AssemblyLoader class.
	/// </remarks>
	/// <see cref="https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/AssemblyLoadContext.ContextualReflection.md" />
	public class AssemblyLoader
	{
		private static System.Collections.Concurrent.ConcurrentDictionary<string, AssemblyLoadContext> ExtensionLoadContexts { get; } = new(StringComparer.OrdinalIgnoreCase);
		private static System.Collections.Concurrent.ConcurrentDictionary<string, AssemblyLoadContext> TypeContextCache { get; } = new(StringComparer.OrdinalIgnoreCase);

		private static Boolean isResolvingEventHooked = false;

		/// <summary>
		/// Load an assembly into the appropriate AssemblyLoadContext. 
		/// </summary>
		/// <param name="path">The full path and filename of the assembly to load.</param>
		/// <returns>The loaded assembly.</returns>
		/// <remarks>
		/// The LoadFrom method checks the path of the assembly and separates plugin assemblies into their own AssemblyLoadContext.  This
		/// allows plugins to use their own version of referenced assemblies, if they are present in the plugin's bin folder, or use the versions
		/// which are shipped with Nucleus Core otherwise.  This method returns NULL if the assembly couldn't be loaded.  This is typically the
		/// case when a dll is present that is not an assembly.
		/// </remarks>
		public static Assembly LoadFrom(string path)
		{
			AssemblyLoadContext assemblyLoadContext;
			string extensionFolder = "";
			string pluginPath;
			// Hook the default assembly load context's Resolving event so that we can override it and load extension assemblies from the correct
			// AssemblyLoadContext.
			if (!isResolvingEventHooked)
			{
				AssemblyLoadContext.Default.Resolving += HandleResolving;
				isResolvingEventHooked = true;
			}

			// Determine whether the assembly is located within an extension folder
			pluginPath = System.IO.Path.GetDirectoryName(path);

			extensionFolder = GetExtensionFolderName(pluginPath);

			// special case:  If a file named disable-private-loadcontext.json exists in the extension root, load the assemblies into the
			// default context.  This is a special workaround for cases where .net components do not work properly in a private load context.
			// https://github.com/dotnet/runtime/issues/1388		 https://github.com/dotnet/runtime/pull/58932
			// While the original issue in the XmlSerializer was fixed for .NET 6, the extension-level option to opt-out of private load contexts
			// is still required by the Elastic Search components, as it has the same issue. 
			if (System.IO.File.Exists(System.IO.Path.Combine(Nucleus.Abstractions.Models.Configuration.FolderOptions.EXTENSIONS_FOLDER, extensionFolder, "disable-private-loadcontext.json")))
			{
				extensionFolder = "";
			}

			// Retrieve or create an assembly load context for the specified extension folder, or use the Default assembly load context if the 
			// assembly is not located in an extension folder.
			if (String.IsNullOrEmpty(extensionFolder))
			{
				assemblyLoadContext = AssemblyLoadContext.Default;
			}
			else
			{
				if (ExtensionLoadContexts.ContainsKey(extensionFolder))
				{
					assemblyLoadContext = ExtensionLoadContexts[extensionFolder];
				}
				else
				{
					assemblyLoadContext = new PluginLoadContext(extensionFolder, pluginPath);
					ExtensionLoadContexts.TryAdd(extensionFolder, assemblyLoadContext);
				}
			}

			// Load the assembly
			try
			{
				// exclude anything in the bin/runtimes/**/native folder, these are not .net assemblies
				if (System.IO.Path.GetDirectoryName(path).ToLower().Replace('/', '\\').Contains("\\runtimes\\") && System.IO.Path.GetDirectoryName(path).ToLower().Replace('/', '\\').EndsWith("\\native"))
				{
					return null;
				}
				else
				{

					// Microsoft design documentation on assembly load contexts is here:
					// https://github.com/dotnet/runtime/blob/main/docs/design/features/AssemblyLoadContext.ContextualReflection.md

					// The code in the HandleResolving event handler (below) is important, as it resolves assemblies from plugin assembly load contexts
					// properly when the Nucleus.Core.Layout.ModuleContentRenderer calls actionInvoker.InvokeAsync().  Without the code in the handler, this
					// throws a System.IO.FileNotFoundException: Could not load file or assembly ... exception, presumably because .InvokeAsync 
					// uses one of the Activator classes or one of the Assembly.Load methods which don't work properly with assembly load contexts.  The code in 
					// HandleResolving ignores the requested assembly load context (which is .Default) and uses the module assembly load context instead.
					return assemblyLoadContext.LoadFromAssemblyPath(path);
				}
			}
			catch
			{
				// not a .net assembly
				return null;
			}
		}

		////private static Boolean IsProtectedAssembly(string assemblyPath)
		////{
		////	string[] protectedAssemblies = 
		////	{ 
		////		nameof(Nucleus.Abstractions),
		////		nameof(Nucleus.Data.Common),
		////		nameof(Nucleus.Data.Sqlite),
		////		nameof(Nucleus.Extensions),
		////		nameof(Nucleus.Core),
		////		"Nucleus.ViewFeatures"
		////	};

		////	string assemblyFileName = System.IO.Path.GetFileNameWithoutExtension(assemblyPath);

		////	return protectedAssemblies.Contains(assemblyFileName, StringComparer.OrdinalIgnoreCase);
		////}

		/// <summary>
		/// Return the extension folder (the folder immediately following "extensions") from a full path.
		/// </summary>
		/// <param name="fullPath"></param>
		/// <returns></returns>
		internal static string GetExtensionFolderName(string fullPath)
		{
			System.IO.DirectoryInfo workingDirectory = new(fullPath);
			while (workingDirectory.Parent != null)
			{
				if (workingDirectory.Parent.Name.Equals(Nucleus.Abstractions.Models.Configuration.FolderOptions.EXTENSIONS_FOLDER, StringComparison.OrdinalIgnoreCase))
				{
					return workingDirectory.Name;
				}
				workingDirectory = workingDirectory.Parent;
			}
			return "";

			//string extensionFolder = "";

			//if (fullPath.StartsWith(System.IO.Path.GetDirectoryName(typeof(AssemblyLoader).Assembly.Location), StringComparison.OrdinalIgnoreCase))
			//{
			//	extensionFolder = "";
			//}
			//else if (fullPath.StartsWith(Nucleus.Abstractions.Folders.GetExtensionsFolder(), StringComparison.OrdinalIgnoreCase))
			//{
			//	// Get the first part of the extension folder.  For example, for the path "/Extensions/TextHtml" the extensionFolder would be "TextHtml"
			//	string[] pluginFolderParts = fullPath.Replace(Nucleus.Abstractions.Folders.GetExtensionsFolder(), "").Split('\\', StringSplitOptions.RemoveEmptyEntries);
			//	extensionFolder = pluginFolderParts[0];
			//}

			//return extensionFolder;
		}

		/// <summary>
		/// Handle the assembly resolving event.  If the specified context is AssemblyLoadContext.Default, check the
		/// AssemblyLoadContext.CurrentContextualReflectionContext and if it is not null, use the
		/// CurrentContextualReflectionContext to load the requested assembly. 
		/// </summary>
		/// <param name="context">Request assembly load context.</param>
		/// <param name="assemblyName">Requested assenmbly.</param>
		/// <remarks>
		/// This code resolves module assemblies from the PluginLoadContext when the Nucleus.Core.Layout.ModuleContentRenderer 
		/// calls actionInvoker.InvokeAsync().  Without this handler, .InvokeAsync throws a System.IO.FileNotFoundException: 
		/// Could not load file or assembly exception because it is using one of the assembly loading methods that are 
		/// not supported by AssemblyLoadContext - like GetType(string typeName), when typeName includes a assembly-qualified type 
		/// reference.  If the requested context isn't .Default or the current context is null or Default,
		/// this code does nothing (returns null) and allows .net to do the work as normal.
		/// </remarks>
		/// <returns></returns>
		private static Assembly HandleResolving(AssemblyLoadContext context, AssemblyName assemblyName)
		{
			if (context == AssemblyLoadContext.Default)
			{
				if (AssemblyLoadContext.CurrentContextualReflectionContext != null)
				{
					Assembly asm = AssemblyLoadContext.CurrentContextualReflectionContext.Assemblies.Where(asm => Compare(asm.GetName(), assemblyName)).FirstOrDefault();
					return asm;
				}
				else
				{
					foreach (AssemblyLoadContext moduleContext in ExtensionLoadContexts.Values)
					{
						Assembly asm = moduleContext.Assemblies.Where(asm => Compare(asm.GetName(), assemblyName)).FirstOrDefault();
						if (asm != null) return asm;
					}
				}
			}

			// returning null from HandleResolving lets other handlers try to find the assembly			
			return null;
		}

		private static Boolean Compare(AssemblyName assemblyName1, AssemblyName assemblyName2)
		{
			return assemblyName1.Name == assemblyName2.Name &&
				(
					(assemblyName1.Version == null || assemblyName2.Version == null) ||
					assemblyName1.Version == assemblyName2.Version
				);
		}

		/// <summary>
		/// Retrieves the AssemblyLoadContext for the specified type and sets the CurrentContextualReflectionContext to it.
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		/// <remarks>
		/// For assemblies located in an extension folder, the ContextualReflectionScope for the extension is set to the 
		/// AssemblyLoadContext for the extension.  For assemblies which are located elsewhere, the Default AssemblyLoadContext
		/// is set.
		/// This method should be called with a using statement.  When the CurrentContextualReflectionContext is set, calls to Type.GetType
		/// and other type-resolving functions use the CurrentContextualReflactionContext.  When the ContextualReflectionScope is
		/// disposed, the CurrentContextualReflectionContext is reset back to the Default AssemblyLoadContext.
		/// </remarks>
		/// <example>
		/// using (System.Runtime.Loader.AssemblyLoadContext.ContextualReflectionScope scope = Nucleus.Core.Plugins.AssemblyLoader.EnterExtensionContext(moduleinfo.ModuleDefinition.ClassTypeName))
		///	{
		///		moduleControllerTypeInfo = Type.GetType(moduleinfo.ModuleDefinition.ClassTypeName).GetTypeInfo();
		///	}
		/// </example>
		public static System.Runtime.Loader.AssemblyLoadContext.ContextualReflectionScope EnterExtensionContext(string typeName)
		{
			if (TypeContextCache.ContainsKey(typeName))
			{
				return TypeContextCache[typeName].EnterContextualReflection();
			}
			else
			{
				// If the type was not found in the cache, loop through the ModuleLoadContexts until it is found (and add it to the cache for next time)
				foreach (AssemblyLoadContext context in ExtensionLoadContexts.Values)
				{
					System.Runtime.Loader.AssemblyLoadContext.ContextualReflectionScope scope = context.EnterContextualReflection();

					if (Type.GetType(typeName) != null)
					{
						TypeContextCache.TryAdd(typeName, context);
						return scope;
					}

					scope.Dispose();
				}

				return System.Runtime.Loader.AssemblyLoadContext.Default.EnterContextualReflection();
			}
		}

		/// <summary>
		/// Unload all AssemblyLoadContexts.
		/// </summary>
		/// <remarks>
		/// This method is used to unload extension AssemblyLoadContexts in order to complete the installation or upgrade of an extension.
		/// </remarks>
		public static void UnloadAll()
		{
			TypeContextCache.Clear();

			string extensionsFolder = Nucleus.Abstractions.Models.Configuration.FolderOptions.GetExtensionsFolderStatic(false);

			if (System.IO.Directory.Exists(extensionsFolder))
			{
				foreach (string path in System.IO.Directory.GetDirectories(extensionsFolder))
				{
					UnloadPlugin(GetExtensionFolderName(path));
				}
			}

			ExtensionLoadContexts.Clear();

			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		/// <summary>
		/// Unload the AssemblyLoadContext for the specified extension.
		/// </summary>
		/// <param name="pluginName"></param>
		private static void UnloadPlugin(string pluginName)
		{
			if (ExtensionLoadContexts.Keys.Contains(pluginName))
			{
				AssemblyLoadContext context = ExtensionLoadContexts[pluginName];

				ExtensionLoadContexts.Remove(pluginName, out AssemblyLoadContext removed);

				context.Unload();
				context = null;

				GC.Collect();
				GC.WaitForPendingFinalizers();

			}
		}

		/// <summary>
		/// Enumerate all Assemblies (dlls) including core assemblies and plugin (extension) assemblies.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This method returns a list of filenames in /bin and /{Extensions}/** with the .dll extension.  Not all dlls are assemblies.  Callers
		/// should implement checking or exception handling for cases where a returned dll file is not an assembly.
		/// </remarks>
		private static IEnumerable<string> EnumerateAssemblyNames()
		{
			// get all assemblies (dlls) in /bin 			
			foreach (string filename in System.IO.Directory.EnumerateFiles(System.IO.Path.GetDirectoryName(typeof(AssemblyLoader).Assembly.Location), "*.dll", System.IO.SearchOption.AllDirectories))
			{
				yield return filename;
			}

			string extensionsFolder = Nucleus.Abstractions.Models.Configuration.FolderOptions.GetExtensionsFolderStatic(false);

			if (System.IO.Directory.Exists(extensionsFolder))
			{
				// get all extension assemblies (dlls) in /extensions/*
				foreach (string foldername in System.IO.Directory.EnumerateFiles(extensionsFolder, "*.dll", System.IO.SearchOption.AllDirectories))
				{
					yield return foldername;
				}
			}
		}

		/// <summary>
		/// Return a list of assemblies which have the assembly attribute specified by T.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <remarks>
		/// A side effect of this function is that assemblies are loaded into their assembly load contexts.
		/// </remarks>
		internal static IEnumerable<string> GetAssemblyNamesWithAttribute<T>()
		{
			//SortedList<string, string> results = new();
			//HashSet<string> results = new();

			foreach (string assemblyFileName in EnumerateAssemblyNames())
			{
				Assembly assembly = AssemblyLoader.LoadFrom(assemblyFileName);

				if (assembly != null)
				{
					CustomAttributeData attributes = assembly.CustomAttributes.FirstOrDefault(assm => typeof(T).IsAssignableFrom(assm.AttributeType));

					if (attributes != null)
					{
						//if (!results.Contains(assemblyFileName))
						//{
						//	results.Add(assemblyFileName);
						yield return assemblyFileName;
						//}
					}
				}
			}

			//return results.Values;
		}

		/// <summary>
		/// Return a list of assemblies which implement the class specified by T.  
		/// </summary>
		/// <typeparam name="T">The type to search for.</typeparam>
		/// <param name="path">The content root path for Nucleus Core.</param>
		/// <returns></returns>
		/// <remarks>
		/// A side effect of this function is that assemblies are loaded into their assembly load contexts by <see cref="LoadAssemblies"/>.
		/// </remarks>
		internal static IEnumerable<Assembly> GetAssembliesImplementing<T>(ILogger logger)
		{
			foreach (Assembly assembly in LoadAssemblies())
			{
				Type type;

				try
				{
					// We use .FirstOrDefault for performance - we want all of the assemblies implementing <T>, but we only need one match
					// not all of them.
					type = assembly.GetTypes().FirstOrDefault
					(
						typ => !typ.IsAbstract && typeof(T).IsAssignableFrom(typ) && !typ.Equals(typeof(T))
					);
				}
				catch (System.Reflection.ReflectionTypeLoadException e)
				{
					logger?.LogError(e, "Unable to load assembly {0}", assembly.FullName);
					type = null;
				}

				if (type != null)
				{
					yield return assembly;
				}
			}
		}

		/// <summary>
		/// Return a list of types which implement the class specified by T, but are not T. 
		/// </summary>
		/// <typeparam name="T">The type to search for.</typeparam>
		/// <returns>A list of .Net types which implement T.</returns>
		/// <remarks>
		/// A side effect of this function is that assemblies are loaded into their assembly load contexts by <see cref="LoadAssemblies"/>.
		/// </remarks>
		public static IEnumerable<Type> GetTypes<T>()
		{
			foreach (Assembly assembly in LoadAssemblies())
			{
				if (assembly != null)
				{
					foreach (Type type in GetTypes(assembly)
						.Where(type => !type.IsAbstract && typeof(T).IsAssignableFrom(type) && !type.Equals(typeof(T))))
					{
						yield return type;
					}
				}
			}
		}

		private static Type[] GetTypes(System.Reflection.Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (System.Reflection.ReflectionTypeLoadException)
			{
				return new Type[] { };
			}
		}

		/// <summary>
		/// Load assemblies in /bin and in extension folders into their assembly load contexts.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// For assemblies located in an extension folder, the assembly is loaded into an AssemblyLoadContext for the extension.  For 
		/// assemblies which are located elsewhere, the Default AssemblyLoadContext is used.
		/// </remarks>
		private static IEnumerable<Assembly> LoadAssemblies()
		{
			List<Assembly> assemblies = new List<Assembly>();

			// All assemblies must be loaded into their correct AssemblyLoadContexts in this function (don't use yield return).  This is to ensure that
			// assemblies which reference other assemblies (in an extension/bin folder) load them from the correct folder and using the correct AssemmblyLoadContext.
			foreach (string assemblyFileName in EnumerateAssemblyNames())
			{
				// Load all assemblies EXCEPT Razor views (because they aren't real assemblies and LoadFrom doesn't 
				// work on them)
				if (!System.IO.Path.GetFileName(assemblyFileName).EndsWith("Views.dll"))
				{
					Assembly assembly = AssemblyLoader.LoadFrom(assemblyFileName);

					if (assembly != null)
					{
						assemblies.Add(assembly);
					}
				}
			}

			return assemblies;
		}

	}
}
