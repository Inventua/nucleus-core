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
		public const string CONTAINERS_FOLDER = SHARED_FOLDER + "\\Containers";

		/// <summary>
		/// Sub folder name (in the web root folder) used for shared layouts
		/// </summary>
		public const string LAYOUTS_FOLDER = SHARED_FOLDER + "\\Layouts";

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

		/// <summary>
		/// Gets the application root folder.
		/// </summary>
		/// <returns></returns>
		public string GetWebRootFolder()
		{
			return System.Environment.CurrentDirectory;
		}

		/// <summary>
		/// Replaces tokens from configuration entries representing a file name, and returns an absolute path.
		/// </summary>
		/// <param name="folder"></param>
		/// <returns></returns>
		public string ParseFolder(string folder)
		{
			System.IO.DirectoryInfo directory = new(Parse(folder));

			if (!directory.Exists)
			{
				directory.Create();
			}

			return directory.FullName;
		}

		/// <summary>
		/// Replaces tokens from any configuration file value.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public string Parse(string value)
		{
			return value
					.Replace("{DataFolder}", DataFolder)
					.Replace("{WebRootFolder}", System.Environment.CurrentDirectory);
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

			appsFolder = System.IO.Path.Combine(System.Environment.CurrentDirectory, EXTENSIONS_FOLDER);

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
			string result = System.IO.Path.Combine(GetExtensionsFolderStatic(create), name);

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
			string result = System.IO.Path.Combine(GetExtensionsFolder(), name);

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
		public string DataFolder { get; set; }

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
			if (create)
			{
				return EnsureExists(System.IO.Path.Combine(this.GetDataFolder(LOG_FOLDER, create), subFolder));
			}
			else
			{
				return System.IO.Path.Combine(this.GetDataFolder(LOG_FOLDER, create), subFolder);
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
			if (create)
			{
				return EnsureExists(System.IO.Path.Combine(this.GetDataFolder(CACHE_FOLDER, create), subFolder));
			}
			else
			{
				return System.IO.Path.Combine(this.GetDataFolder(CACHE_FOLDER, create), subFolder);
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
			string folderName = System.IO.Path.Combine(this.DataFolder, subFolder);

			if (create)
			{
				return EnsureExists(folderName);
			}
			else
			{
				return folderName;
			}
		}

	}
}
