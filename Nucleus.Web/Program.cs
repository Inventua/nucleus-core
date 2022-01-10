using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Nucleus.Core.Plugins;
using Nucleus.Core.Logging;

namespace Nucleus.Web
{
	public class Program
	{
		/// <summary>
		/// Specifies whether to restart in a loop, or exit the loop and terminate
		/// </summary>
		private static bool doRestart = true;

		/// <summary>
		/// Specifies whether this loop is a restart, or is the initial (first) time around
		/// </summary>
		private static bool isRestart = false;

		/// <summary>
		/// Application host instance
		/// </summary>
		private static IHost WebHost;

		///// <summary>
		///// File system watcher used to monitor the extensions folder for changes to DLL files and restart the application host if changes
		///// are detected.
		///// </summary>
		//private static System.IO.FileSystemWatcher Watcher;
				
		public static void Main(string[] args)
		{
			// The app can start with the current directory set to /bin, or /bin/debug/net5.0 but we want it set to the application root
			System.IO.DirectoryInfo workingDirectory = new System.IO.DirectoryInfo(System.Environment.CurrentDirectory);
			while (workingDirectory.Parent != null)
			{
				if (workingDirectory.Name.Equals("bin", StringComparison.OrdinalIgnoreCase))
				{
					System.Environment.CurrentDirectory = workingDirectory.Parent.FullName;
					break;
				}
				workingDirectory = workingDirectory.Parent;
			}
						
			while (doRestart)
			{
				if (isRestart)
				{
					WebHost.Dispose();
					//WebHost?.Logger().LogInformation($"Restarted at {DateTime.Now}.");

					Nucleus.Core.Plugins.AssemblyLoader.UnloadAll();
					WebHost.StopAsync();
					
					System.Threading.Tasks.Task.Delay(5000).Wait();
				}

				isRestart = false;
				doRestart = false;

				Nucleus.Core.Managers.ExtensionManager.CleanupBackups(WebHost?.Logger());
				
				WebHost = CreateHostBuilder(args)
					.ConfigureAppConfiguration(Startup.ConfigureAppConfiguration)
					.Build();
				//WatchFileChanges();
				WebHost.Run();
			}
		}

		/// <summary>
		/// Initialize the application.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			string hostingStartupAssemblies = string.Join(";", PluginExtensions.GetHostingStartupAssemblies(WebHost?.Logger()));
						
			return Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseSetting	(WebHostDefaults.HostingStartupAssembliesKey, hostingStartupAssemblies);
					webBuilder.UseStartup<Startup>();
				});
		}

		/// <summary>
		/// File system watcher Changed event handler.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		//static async void FileChanged(object sender, FileSystemEventArgs e)
		//{
		//	WebHost.Logger().LogInformation($"Detected assembly file change {e.FullPath} [{e.ChangeType}].");

		//	// The restart loop is disabled at present, because AssemblyLoadContexts are not unloading assemblies properly, which 
		//	// prevents cleanup tasks from running successfully - so the application terminates after an extension install/uninstall.  
		//	//doRestart = true;
		//	//isRestart = true;
			
		//	//await System.Threading.Tasks.Task.Delay(10000);			
		//	await WebHost.StopAsync();
		//}

		/// <summary>
		/// Sets up the file system watcher to monitor for changes to dll files in extensions folders.
		/// </summary>
		//static void WatchFileChanges()
		//{
		//	if (Watcher == null)
		//	{
		//		string extensionsFolder = Nucleus.Abstractions.Folders.GetExtensionsFolder();

		//		Watcher = new System.IO.FileSystemWatcher(extensionsFolder, "*.dll")
		//		{
		//			EnableRaisingEvents = true,
		//			IncludeSubdirectories = true
		//		};

		//		Watcher.Changed += FileChanged;
		//		Watcher.Deleted += FileChanged;
		//		Watcher.Created += FileChanged;
		//		Watcher.Renamed += FileChanged;
		//	}
		//}
	}
}
