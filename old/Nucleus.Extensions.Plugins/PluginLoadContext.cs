//using System;
//using System.Reflection;
//using System.Runtime.Loader;
//using System.ComponentModel;

//namespace Nucleus.Extensions.Plugins
//{
//	/// <summary>
//	/// Implementation of AssemblyLoadContext used load OsaAppCore plugins.
//	/// </summary>
//	/// <remarks>
//	/// This class is used by OsaAppCore.  OsaAppCore applications (plugins) would not typically need to use the methods
//	/// of the PluginLoadContext class.
//	/// </remarks>
//	class PluginLoadContext : AssemblyLoadContext
//	{
//		private AssemblyDependencyResolver Resolver { get; }

//		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
//		public PluginLoadContext(string pluginPath)
//		{
//			this.Resolver = new AssemblyDependencyResolver(pluginPath);
//		}

//		//protected override Assembly Load(AssemblyName assemblyName)
//		//{
//		//	string assemblyPath = this.Resolver.ResolveAssemblyToPath(assemblyName);
//		//	if (assemblyPath != null)
//		//	{
//		//		return LoadFromAssemblyPath(assemblyPath);
//		//	}

//		//	return null;
//		//}

//		//protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
//		//{
//		//	string libraryPath = this.Resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
//		//	if (libraryPath != null)
//		//	{
//		//		return LoadUnmanagedDllFromPath(libraryPath);
//		//	}

//		//	return IntPtr.Zero;
//		//}
//	}

//}
