using System;
using System.Reflection;
using System.Runtime.Loader;
using System.ComponentModel;

namespace Nucleus.Core.Plugins
{
	/// <summary>
	/// Implementation of AssemblyLoadContext used to load plugins.
	/// </summary>
	/// <remarks>
	/// This class is used by Nucleus Core.  Extensions would not typically need to use the methods of the 
	/// PluginLoadContext class.
	/// </remarks>
	class PluginLoadContext : AssemblyLoadContext, IDisposable
	{
		private AssemblyDependencyResolver Resolver { get; set; }

		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		public PluginLoadContext(string pluginPath) : base(true)
		{
			this.Resolver = new AssemblyDependencyResolver(pluginPath);
		}

		/// <summary>
		/// Load an assembly into this AssemblyLoadContext from a stream.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		/// <remarks>
		/// This method is used by the extension installer, in order to extract version information.
		/// </remarks>
		public new System.Reflection.Assembly LoadFromStream(System.IO.Stream stream)
		{
			// base.LoadFromStream requires a seekable stream, so we copy to a new MemoryStream first
			System.IO.MemoryStream data = new();
			stream.CopyTo(data);
			data.Position = 0;

			return base.LoadFromStream(data);
		}

		public void Dispose()
		{
			this.Resolver = null;
		}
	}

}
