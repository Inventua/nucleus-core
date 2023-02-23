using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Represents application folders.
	/// </summary>
	public class FolderOptions
	{
		/// <summary>
		/// Configuration file section.
		/// </summary>
		public const string Section = "Nucleus:FolderOptions";

		// Data paths    
		/// <summary>
		/// Sub folder name (within the Nucleus data folder) used for temp files
		/// </summary>
		private const string TEMP_FOLDER = "Temp";

    // Data paths    
    /// <summary>
    /// Sub folder name (within the Nucleus temp folder) used for extensions that should be automatically installed
    /// </summary>
    public const string EXTENSIONS_AUTO_INSTALL_FOLDER = "Auto-Install";

    // Data paths    
    /// <summary>
    /// Sub folder name (within the Nucleus data folder) used for log files
    /// </summary>
    private const string LOG_FOLDER = "Logs";

		// Data paths    
		/// <summary>
		/// Sub folder name (within the Nucleus data folder) used for cache files
		/// </summary>
		private const string CACHE_FOLDER = "Cache";

		// Application paths

		/// <summary>
		/// Sub folder name (in the web root folder) used for shared files
		/// </summary>
		public const string SHARED_FOLDER = "Shared";

		/// <summary>
		/// Sub folder name (in the web root folder) used for shared containers
		/// </summary>
		public const string CONTAINERS_FOLDER = SHARED_FOLDER + "/Containers";

		/// <summary>
		/// Sub folder name (in the web root folder) used for shared layouts
		/// </summary>
		public const string LAYOUTS_FOLDER = SHARED_FOLDER + "/Layouts";

		/// <summary>
		/// Sub folder name (in the web root folder) used for areas
		/// </summary>
		public const string AREAS_FOLDER = "Areas";

		/// <summary>
		/// Sub folder name (in the web root folder) used for extensions
		/// </summary>
		public const string EXTENSIONS_FOLDER = "Extensions";

		/// <summary>
		/// Sub folder name (in the web root folder) used for shared resources (css, js and third-party libraries)
		/// </summary>
		public const string RESOURCES_FOLDER = "Resources";

		/// <summary>
		/// Array of allowed static file paths
		/// </summary>
		public static string[] ALLOWED_STATICFILE_PATHS = new[]
		{
			Nucleus.Abstractions.Models.Configuration.FolderOptions.RESOURCES_FOLDER,
			Nucleus.Abstractions.Models.Configuration.FolderOptions.EXTENSIONS_FOLDER,
			Nucleus.Abstractions.Models.Configuration.FolderOptions.SHARED_FOLDER,
			Nucleus.Abstractions.Models.Configuration.FolderOptions.AREAS_FOLDER
		};

		private static string WebRootFolder { get; } = NormalizePath(System.Environment.CurrentDirectory);

		/// <summary>
		/// Gets the application root folder.
		/// </summary>
		/// <returns></returns>
		public static string GetWebRootFolder()
		{
			return WebRootFolder;
		}

		/// <summary>
		/// Replaces tokens from configuration entries representing a file name, and returns an absolute path.
		/// </summary>
		/// <param name="folderName"></param>
		/// <returns></returns>
		public string ParseFolder(string folderName)
		{
			System.IO.DirectoryInfo directory = new(Parse(folderName));

			if (!directory.Exists)
			{
				directory.Create();
			}

			return NormalizePath(directory.FullName);
		}

		/// <summary>
		/// Replaces tokens from any configuration file value.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string Parse(string value)
		{
			return Environment.ExpandEnvironmentVariables(value)
					.Replace("{DataFolder}", DataFolder)
					.Replace("{WebRootFolder}", GetWebRootFolder());
		}

		/// <summary>
		/// Gets the full path to the extensions folder.
		/// </summary>
		/// If the folder does not exist, it is created.
		/// <returns></returns>
		public string GetExtensionsFolder()
		{
			return GetExtensionsFolder(true);
		}

		/// <summary>
		/// Gets the full path to the extensions folder.
		/// </summary>
		/// <param name="create">Specifies whether to create the folder if it does not already exist.</param>
		/// <returns></returns>
		public string GetExtensionsFolder(Boolean create)
		{
			return GetExtensionsFolderStatic(create);
		}

		/// <summary>
		/// Gets the full path to the extensions folder.
		/// </summary>
		/// <returns></returns>
		public static string GetExtensionsFolderStatic(Boolean create)
		{
			string appsFolder;

			appsFolder = NormalizePath(System.IO.Path.Combine(WebRootFolder, EXTENSIONS_FOLDER));

			if (create)
			{
				return EnsureExistsStatic(appsFolder);
			}

			return appsFolder;
		}

		/// <summary>
		/// Gets a sub-folder of the extensions folder.
		/// </summary>
		/// <param name="name">The name of your extension folder, relative to the extensions folder root.</param>
		/// <param name="create">Specifies whether to create the folder if it does not already exist.</param>
		/// <returns></returns>
		public static string GetExtensionFolderStatic(string name, Boolean create)
		{
			string result = NormalizePath(System.IO.Path.Combine(GetExtensionsFolderStatic(create), name));

			if (create)
			{
				if (!System.IO.Directory.Exists(result))
				{
					System.IO.Directory.CreateDirectory(result);
				}
			}

			return result;
		}

		/// <summary>
		/// Gets a sub-folder of the extensions folder.
		/// </summary>
		/// <param name="name">The name of your extension folder, relative to the extensions folder root.</param>
		/// <param name="create">Specifies whether to create the folder if it does not already exist.</param>
		/// <returns></returns>
		public string GetExtensionFolder(string name, Boolean create)
		{
			string result = NormalizePath(System.IO.Path.Combine(GetExtensionsFolder(), name));

			if (create)
			{
				EnsureExists(result);
			}

			return result;
		}

		/// <summary>
		/// Gets the default application data storage folder location, used for logs and database files.
		/// </summary>
		/// <returns></returns>
		private static string DataFolder { get; set; }

		/// <summary>
		/// Sets the data folder to a default value if its current value is empty, after replacing environment variables.
		/// </summary>
		/// <param name="ensureExists">Specifies whether to ensure that the path exists by creating the folder if it does not already exist.</param>
		public string SetDefaultDataFolder(Boolean ensureExists)
		{
			const string DEFAULT_FOLDER = "%ProgramData%/Nucleus";

      DataFolder = NormalizePath(Parse(String.IsNullOrEmpty(DataFolder) ? DEFAULT_FOLDER : DataFolder));
						
			if (ensureExists)
			{
				EnsureExists(DataFolder);
			}

			return DataFolder;
		}

    /// <summary>
    /// Sets the data folder to the specified value, after replacing environment variables.
    /// </summary>
    /// <param name="value">Data folder path</param>
    /// <param name="ensureExists">Specifies whether to ensure that the path exists by creating the folder if it does not already exist.</param>
    /// <remarks>
    /// This function is intended for use in a workaround to facilitate startup logging, and should not be called by anything 
    /// except LoggingBuilderExtensions.ConfigureTextFileLogger.
    /// </remarks>
    /// <internal>
    /// This function is intended for use in a workaround to facilitate startup logging, and should not be called by anything 
    /// except LoggingBuilderExtensions.ConfigureTextFileLogger.
    /// </internal>
    public string SetDataFolder(string value, Boolean ensureExists)
    {
      DataFolder = value;
      return SetDefaultDataFolder(ensureExists);
    }

    /// <summary>
    /// Check whether the specified folder exists, and create it if it does not.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public string EnsureExists(string path)
		{
			return EnsureExistsStatic(path);
		}

		/// <summary>
		/// Check whether the specified folder exists, and create it if it does not.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static string EnsureExistsStatic(string path)
		{
			if (!System.IO.Directory.Exists(path))
			{
				System.IO.Directory.CreateDirectory(path);
			}
			
			return path;
		}

		/// <summary>
		/// Gets the root data storage folder location.
		/// </summary>
		/// <returns></returns>
		public string GetDataFolder()
		{
			return DataFolder;
		}

		/// <summary>
		/// Gets the data storage folder location for temporary files.
		/// </summary>
		/// <remarks>
		/// If the folder does not exist, it is created.
		/// </remarks>
		/// <returns></returns>
		public string GetTempFolder()
		{
			return this.GetTempFolder(true);
		}

		/// <summary>
		/// Gets the data storage folder location for temporary files.
		/// </summary>
		/// <param name="create">Specifies whether to check that the folder exists and create it if not.</param>
		/// <returns></returns>
		public string GetTempFolder(Boolean create)
		{
			return this.GetDataFolder(TEMP_FOLDER, create);
		}

    /// <summary>
    /// Gets the folder which contains extension packages which are to be automatically installed.
    /// </summary>
    /// <returns></returns>
    public string GetAutoInstallExtensionsFolder()
    {
      return System.IO.Path.Join(this.GetTempFolder(true), EXTENSIONS_AUTO_INSTALL_FOLDER);
    }

		/// <summary>
		/// Gets the data storage folder location for log files.
		/// </summary>
		/// <remarks>
		/// If the folder does not exist, it is created.
		/// </remarks>
		/// <returns></returns>
		public string GetLogFolder()
		{
			return this.GetLogFolder(true);
		}

		/// <summary>
		/// Gets the data storage folder location for log files.
		/// </summary>
		/// <remarks>
		/// <param name="create">Specifies whether to check that the folder exists and create it if not.</param>
		/// </remarks>
		/// <returns></returns>
		public string GetLogFolder(Boolean create)
		{
			return this.GetDataFolder(LOG_FOLDER, create);
		}

		/// <summary>
		/// Gets the data storage folder location for log files.
		/// </summary>
		/// <param name="subFolder"/>
		/// <remarks>
		/// If the folder does not exist, it is created.
		/// </remarks>
		/// <returns></returns>
		public string GetLogFolder(string subFolder)
		{
			return this.GetLogFolder(subFolder, true);
		}

		/// <summary>
		/// Gets the data storage folder location for log files.
		/// </summary>
		/// <remarks>
		/// <param name="subFolder"/>
		/// <param name="create">Specifies whether to check that the folder exists and create it if not.</param>
		/// </remarks>
		/// <returns></returns>
		public string GetLogFolder(string subFolder, Boolean create)
		{
      string folderName = NormalizePath(System.IO.Path.Combine(this.GetDataFolder(LOG_FOLDER, create), subFolder));
			if (create)
			{
				return EnsureExists(folderName);
			}
			else
			{
				return folderName;
			}
		}

		/// <summary>
		/// Gets the data storage folder location for cache files.
		/// </summary>
		/// <remarks>
		/// If the folder does not exist, it is created.
		/// </remarks>
		/// <returns></returns>
		public string GetCacheFolder()
		{
			return this.GetCacheFolder(true);
		}

		/// <summary>
		/// Gets the data storage folder location for cache files.
		/// </summary>
		/// <param name="create">Specifies whether to check that the folder exists and create it if not.</param>
		/// <returns></returns>
		public string GetCacheFolder(Boolean create)
		{
			return this.GetDataFolder(CACHE_FOLDER, create);
		}

		/// <summary>
		/// Gets the data storage folder location for cache files and appends the specified sub-folder.
		/// </summary>
		/// <remarks>
		/// If the folder does not exist, it is created.
		/// </remarks>
		/// <returns></returns>
		public string GetCacheFolder(string subFolder)
		{
			return GetCacheFolder(subFolder, true);			
		}

		/// <summary>
		/// Gets the data storage folder location for cache files and appends the specified sub-folder.
		/// </summary>
		/// <param name="subFolder">Specifies a sub-folder of the cache folder.</param>
		/// <param name="create">Specifies whether to check that the folder exists and create it if not.</param>
		/// <returns></returns>
		public string GetCacheFolder(string subFolder, Boolean create)
		{
      string folderName = NormalizePath(System.IO.Path.Combine(this.GetDataFolder(CACHE_FOLDER, create), subFolder));

      if (create)
			{
				return EnsureExists(folderName);
			}
			else
			{
				return folderName;
			}
		}

		/// <summary>
		/// Gets an application data storage folder sub-folder. 
		/// </summary>
		/// <param name="subFolder"></param>
		/// <param name="create">Specifies whether to check that the folder exists and create it if not.</param>
		/// <remarks>
		/// If the folder does not exist, it is created.
		/// </remarks>
		/// <returns></returns>
		public string GetDataFolder(string subFolder, Boolean create)
		{
			string folderName = NormalizePath(System.IO.Path.Combine(DataFolder, subFolder).Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));

			if (create)
			{
				return EnsureExists(folderName);
			}
			else
			{
				return folderName;
			}
		}

    /// <summary>
    /// Return the specified path with path separator "\" characters replaced by "/" so that the path will work in 
    /// both Windows and Linux.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string NormalizePath(string path)
    {
      if (String.IsNullOrEmpty(path)) return path;
      return path.Replace("\\", "/");      
    }
  }
}
