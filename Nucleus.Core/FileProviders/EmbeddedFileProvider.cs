using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace Nucleus.Core.FileProviders
{
	/// <summary>
	/// The Embedded File Provider retrieves resources (files) which are embedded within compiled Razor assemblies.  
	/// </summary>
	/// <remarks>
	/// The Embedded File Provider augments the Microsoft EmbeddedFileProvider: paths are not case-sensitive, and if you follow the .net 
	/// convention of adding your embedded resources to a &quot;Resources&quot; folder (or even if you choose another root folder to put 
	/// your resources in) you do not have to include the &quot;root&quot; folder in the requested path.<br/><br/>
	/// For example: If you have an embedded resource named &quot;image.gif&quot; which is in &quot;Resources\images&quot; in your Visual Studio project, which 
	/// is located at runtime in /apps/my-app/bin, using the Microsoft embedded file provider, your application would have to specify the path 
	/// &quot;~/apps/my-app/Resources/image.gif&quot;.  <br/><br/> 
	/// Using the Nucleus Core embedded file provider means that you can use the simpler form: 
	/// &quot;~/my-app/image.gif&quot;.<br/><br/>
	/// The Embedded File Provider service is injected by the Nucleus Core host and is always available in all Nucleus Core applications.  
	/// </remarks>
	public class EmbeddedFileProvider : Microsoft.Extensions.FileProviders.IFileProvider
	{
		private IHttpContextAccessor Context { get; }
		private ILogger<EmbeddedFileProvider> Logger { get; }

		private Assembly EmbeddedAssembly { get; }
		private string[] AssemblyResources { get; }
		private string PluginFolder { get; }
		private Microsoft.Extensions.FileProviders.EmbeddedFileProvider Base { get; }
		private string ResourcesNamespace { get; }
		private ConcurrentDictionary<string, string> ResolvedResourcesCache { get; } = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		public EmbeddedFileProvider(IHttpContextAccessor Context, ILogger<EmbeddedFileProvider> Logger, Assembly assembly, string pluginFolder)
		{
			this.Base = new Microsoft.Extensions.FileProviders.EmbeddedFileProvider(assembly, "");
			this.EmbeddedAssembly = assembly;
			this.AssemblyResources = this.EmbeddedAssembly.GetManifestResourceNames();
			this.Context = Context;
			this.Logger = Logger;

			this.PluginFolder = VerifyResourceName(pluginFolder.StartsWith("/") || string.IsNullOrEmpty(pluginFolder) ? "" : "/" + pluginFolder);//.ToLower();

			this.ResourcesNamespace = FindNamespace();
		}

		private string FindNamespace()
		{
			string ns = "";
			string prefix;
			Boolean found = false;

			if (this.AssemblyResources.Length <= 1)
			{
				return "";  // no resources, or only one resource, so we can't find the namespace that they have in common
			}
			else
			{
				while (!found)
				{
					if (this.AssemblyResources[0].Substring(ns.Length).IndexOf(".") > 0)
					{
						prefix = this.AssemblyResources[0].Substring(0, ns.Length + this.AssemblyResources[0].Substring(ns.Length).IndexOf(".") + 1);
					}
					else
					{
						return "";
					}

					if (AllEmbeddededResourcesStartsWith(this.AssemblyResources, prefix))
					{
						ns = prefix;						
					}
					else
					{
						break;
					}
				}
			}
						
			if (ns.EndsWith('.'))
			{
				ns = ns.Substring(0, ns.Length - 1);
			}

			return ns;
		}

		private static Boolean AllEmbeddededResourcesStartsWith(string[] resources, string prefix)
		{
			foreach (string resource in resources)
			{
				if (!resource.StartsWith(prefix))
					return false;
			}
			return true;
		}


		// replace characters in the requested path with the characters used for resource naming.
		// The rules are documented in the page for StronglyTypedResourceBuilder.VerifyResourceName().
		// https://docs.microsoft.com/en-us/dotnet/api/system.resources.tools.stronglytypedresourcebuilder.verifyresourcename?redirectedfrom=MSDN&view=netframework-4.8#System_Resources_Tools_StronglyTypedResourceBuilder_VerifyResourceName_System_String_System_CodeDom_Compiler_CodeDomProvider_
		// Also sample code is at https://github.com/dotnet/msbuild/blob/master/src/Tasks/system.design/stronglytypedresourcebuilder.cs
		// HOWEVER: the documented rules and sample code do not appear to be accurate (at least, for .net core), as the *actual* resource names
		// have reserved characters replaced with a dot (.) not an underscore as documented.
		private static String VerifyResourceName(String name)
		{			
			// replace characters that map to underscore
			foreach (char c in new char[] { ' ' })
			{
				name = name.Replace(c, '_');
			}

			// replace characters that map to '.'
			foreach (char c in new char[] { '/' })
			{
				name = name.Replace(c, '.');
			}


			return name;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		public Microsoft.Extensions.FileProviders.IFileInfo GetFileInfo(string subpath)
		{		
			string resourcePath;
			string manifestResourceName = "";

			if (!String.IsNullOrEmpty(this.Context.HttpContext.Request.PathBase))
			{
				//resourcePath = VerifyResourceName(subpath.Replace(this.Context.HttpContext.Request.PathBase, ""));
				if (subpath.StartsWith(this.Context.HttpContext.Request.PathBase, StringComparison.OrdinalIgnoreCase))
				{
					resourcePath = VerifyResourceName(subpath.Substring(this.Context.HttpContext.Request.PathBase.Value.Length));
				}
				else
				{
					resourcePath = VerifyResourceName(subpath);
				}	
			}
			else
			{
				resourcePath = VerifyResourceName(subpath);
			}

			this.Logger.LogTrace("Received request for {0}. Checking {1} - PluginFolder: {2}, Namespace: {3}", subpath, this.EmbeddedAssembly.GetName().Name, this.PluginFolder, this.ResourcesNamespace);

			if (this.AssemblyResources.Length == 0)
				return new Microsoft.Extensions.FileProviders.NotFoundFileInfo(subpath);

			Logger.LogInformation("Received Request for {0}", subpath);
			this.ResolvedResourcesCache.TryGetValue(resourcePath, out manifestResourceName);

			if (!String.IsNullOrEmpty(manifestResourceName))
			{
				Logger.LogInformation("Served {0} from cache", subpath);
				return this.Base.GetFileInfo(manifestResourceName);
			}
			else
			{
				// Check the base assembly for any resources which match subpath, but ignore the namespace
				foreach (string resource in this.AssemblyResources)
				{
					string resourceFile;
					if (string.IsNullOrEmpty(this.ResourcesNamespace))
					{
						resourceFile = resource;
					}
					else
					{
						resourceFile = resource.Replace(this.ResourcesNamespace, "");
					}

					if (string.IsNullOrEmpty(this.PluginFolder))
					{
						if (resourceFile.EndsWith(resourcePath, StringComparison.OrdinalIgnoreCase))
						{						
							Logger.LogInformation("Found {0} in {1}", subpath, this.EmbeddedAssembly.GetName().Name);

							ResolvedResourcesCache.TryAdd(resourcePath, resource);
							return this.Base.GetFileInfo(resource);
						}
					}
					else if (resourcePath.StartsWith(this.PluginFolder, StringComparison.OrdinalIgnoreCase))
					{
						if (resourceFile.EndsWith(resourcePath.Substring(this.PluginFolder.Length), StringComparison.OrdinalIgnoreCase))
						{
							Logger.LogInformation("Found {0} in {1}", subpath, this.EmbeddedAssembly.GetName().Name);

							ResolvedResourcesCache.TryAdd(resourcePath, resource);
							return this.Base.GetFileInfo(resource);
						}
					}
				}
			}

			return this.Base.GetFileInfo(subpath);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		public Microsoft.Extensions.FileProviders.IDirectoryContents GetDirectoryContents(string subpath)
		{
			return null;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		public Microsoft.Extensions.Primitives.IChangeToken Watch(string pattern)
		{
			return null;
		}

	}
}
