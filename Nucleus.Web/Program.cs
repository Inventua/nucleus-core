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
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models.Configuration;
using System.Threading.Tasks;

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
    /// Specifies that an event (like an auto-upgrade) occurred, and the application should shutdown so that the
    /// host can restart it.
    /// </summary>
    private static bool doTerminate = false;

    /// <summary>
    /// Application host instance
    /// </summary>
    private static IHost WebHost;

    /// <summary>
    /// File system watcher used to monitor the extensions folder for changes to DLL files and restart the application host if changes
    /// are detected.
    /// </summary>
    private static System.IO.FileSystemWatcher Watcher;

    public static async Task Main(string[] args)
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
          // this does nothing in Windows but in Linux it makes systemd aware when the application has started/is stopping, and configures logs
          // to be sent in a way that journald (the logging system of systemd) understands log priorities.
          .UseSystemd()
          .Build();

        if (await CheckForAutoUpdates(WebHost))
        {
          // shut down to clean up
          WebHost?.Logger().LogInformation("Restarting after extensions auto-install.");
          doTerminate = true;
        }

        if (!doTerminate)
        {
          LaunchBrowser(args);

          // Disabled the file system watcher, because Assembly load contexts are not unloading properly.  This disables the restart loop because doRestart 
          // won't get set to true (which otherwise happens in FileChanged()).
          // WatchFileChanges(WebHost.Services.GetService<IOptions<Nucleus.Abstractions.Models.Configuration.FolderOptions>>().Value.GetExtensionsFolder());

          // Start the application
          WebHost.Run();
        }        
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
              argIndex++;
            }
            else
            {
              WebHost?.Logger().LogError("Invalid launch args: --launchurl does not have an url specified.");
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
          webBuilder.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, hostingStartupAssemblies);
          webBuilder.UseStartup<Startup>();
        });
    }

    /// <summary>
    /// Check for extension updates in Temp/Auto-Install and install them.  Return true if at least one extension was 
    /// installed.
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public static async Task<Boolean> CheckForAutoUpdates(IHost host)
    {
      Boolean result = false;
      IExtensionManager extensionManager = host.Services.GetService<IExtensionManager>();
      IOptions<FolderOptions> folderOptions = host.Services.GetService<IOptions<FolderOptions>>();

      if (System.IO.Directory.Exists(folderOptions.Value.GetAutoInstallExtensionsFolder()))
      {
        foreach (string extensionFile in System.IO.Directory.EnumerateFiles(folderOptions.Value.GetAutoInstallExtensionsFolder(), "*.zip"))
        {
          try
          {
            using (System.IO.Stream extensionPackage = System.IO.File.OpenRead(extensionFile))
            {
              string location = await extensionManager.SaveTempFile(extensionPackage);
              await extensionManager.InstallExtension(location);
            }

            result = true;
          }
          catch (Exception ex)
          {
            // if an exception occurs during auto-install, we don't want it to prevent the application from starting
            host.Logger().LogError(ex, "Auto-installing {extensionFilename}", extensionFile);
          }

          // remove the extension installer file, whether it has been installed or not.  This prevents bad install sets
          // from staying behind forever.
          try
          {
            System.IO.File.Delete(extensionFile);
          }
          catch (Exception ex)
          {
            host.Logger().LogError(ex, "Deleting {extensionFilename}", extensionFile);
            // suppress to stop exceptions which occur while deleting auto-install sets from disrupting app startup
          }
        }
      }

      return result;
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
