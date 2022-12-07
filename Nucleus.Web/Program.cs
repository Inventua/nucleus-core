using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nucleus.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IO;
using Nucleus.Core.Plugins;
using Nucleus.Core.Logging;
using DocumentFormat.OpenXml.Wordprocessing;
using Org.BouncyCastle.Tls;

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

		/// <summary>
		/// File system watcher used to monitor the extensions folder for changes to DLL files and restart the application host if changes
		/// are detected.
		/// </summary>
		private static System.IO.FileSystemWatcher Watcher;

		public static void Main(string[] args)
		{
			// The app can start with the current directory set to /bin, or /bin/debug/net5.0 but we want it set to the application root.  By
			// design, this code has no effect in a production environment, because the app will start in the application root, and there
			// won't be a /bin folder.
			System.IO.DirectoryInfo workingDirectory = new(System.Environment.CurrentDirectory);
			while (workingDirectory.Parent != null)
			{
				//if (workingDirectory.Name.Equals("bin", StringComparison.OrdinalIgnoreCase))
				if (workingDirectory.EnumerateFiles("appSettings.json").Any())
				{
					System.Environment.CurrentDirectory = workingDirectory.FullName;
					break;
				}
				workingDirectory = workingDirectory.Parent;
			}

			while (doRestart)
			{
				if (isRestart)
				{
					WebHost.Dispose();
					WebHost?.Logger().LogInformation("Restarted at {now}.", DateTime.Now);

					Nucleus.Core.Plugins.AssemblyLoader.UnloadAll();
					WebHost.StopAsync().Wait();
					
					System.Threading.Tasks.Task.Delay(5000).Wait();
				}

				isRestart = false;
				doRestart = false;

				Nucleus.Core.Managers.ExtensionManager.CleanupBackups(WebHost?.Logger());
				
				WebHost = CreateHostBuilder(args)
					.UseContentRoot(Directory.GetCurrentDirectory())
					.ConfigureAppConfiguration(Startup.ConfigureAppConfiguration)
					.Build();

        LaunchBrowser(args);

				// Disabled file system watcher, because Assembly load contexts are not unloading properly.  This disables the restart loop because doRestart 
				// won't get set to true (which otherwise happens in FileChanged()).
				// WatchFileChanges(WebHost.Services.GetService<IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions>>().Value.GetExtensionsFolder());
				WebHost.Run();				
			}
		}

    public static void LaunchBrowser(string[] args)
    {
      Boolean doLaunchUrl = false;
      string launchUrl = null;

      for (int argIndex = 0; argIndex < args.Length; argIndex++)
      {
        string arg = args[argIndex];
        switch (arg)
        {
          case "--launchurl":
            doLaunchUrl = true;
            if (args.Length >= argIndex + 1)
            {
              launchUrl = args[argIndex + 1];
            }
            break;
        }
      }

      if (doLaunchUrl)
      {
        if (!String.IsNullOrEmpty(launchUrl))
        {
          try
          { 
            System.Diagnostics.Process.Start
            (
              new System.Diagnostics.ProcessStartInfo(launchUrl)
              {
                UseShellExecute = true
              }
            );
          }
          catch (Exception ex)
          {
            WebHost?.Logger().LogInformation("Failed to launch '{url}': {message}.", launchUrl, ex.Message);
          } 
        }
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
		static async void FileChanged(object sender, FileSystemEventArgs e)
		{
			WebHost.Logger().LogInformation("Detected assembly file change {fullPath} [{changeType}].", e.FullPath, e.ChangeType);

			doRestart = true;
			isRestart = true;

			await System.Threading.Tasks.Task.Delay(10000);			
			await WebHost.StopAsync();
		}

		/// <summary>
		/// Sets up the file system watcher to monitor for changes to dll files in extensions folders.
		/// </summary>
		static void WatchFileChanges(string extensionsFolder)
		{
			if (Watcher == null)
			{
				//string extensionsFolder = Nucleus.Abstractions.Folders.GetExtensionsFolder();

				Watcher = new System.IO.FileSystemWatcher(extensionsFolder, "*.dll")
				{
					EnableRaisingEvents = true,
					IncludeSubdirectories = true
				};

				Watcher.Changed += FileChanged;
				Watcher.Deleted += FileChanged;
				Watcher.Created += FileChanged;
				Watcher.Renamed += FileChanged;
			}
		}
	}
}
