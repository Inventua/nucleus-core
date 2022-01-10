using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions
{  
  /// <summary>
  /// Represents application folders.
  /// </summary>
	public static class Folders
	{    
    // Data paths    
    public const string TEMP_FOLDER = "Temp";

    // Application paths
    public const string SHARED_FOLDER = "Shared";
    public const string CONTAINERS_FOLDER = SHARED_FOLDER + "\\Containers";
    public const string LAYOUTS_FOLDER = SHARED_FOLDER + "\\Layouts";
    public const string EXTENSIONS_FOLDER = "Extensions";
    public const string RESOURCES_FOLDER = "Resources";

    /// <summary>
    /// Gets the application root folder.
    /// </summary>
    /// <returns></returns>
    static public string GetWebRootFolder()
		{
      return System.Environment.CurrentDirectory;
    }

    /// <summary>
    /// Replaces tokens from configuration files and returns the parsed value.
    /// </summary>
    /// <param name="folder"></param>
    /// <returns></returns>
    static public string Parse(string folder)
		{
      System.IO.DirectoryInfo directory = new(
        folder
          .Replace("{DataFolder}", GetDataFolder())
          .Replace("{WebRootFolder}", System.Environment.CurrentDirectory));

      return directory.FullName;      
		}

  ////  /// <summary>
  ////  /// Gets the 
  ////  /// </summary>
  ////  /// <returns></returns>
  ////  static public string GetLayoutsFolder()
		////{
  ////    return System.IO.Path.Combine(System.Environment.CurrentDirectory, Folders.LAYOUTS_FOLDER);
  ////  }

    /// <summary>
    /// Gets the full path to the extensions folder.
    /// </summary>
    /// <returns></returns>
    static public string GetExtensionsFolder()
    {
      string appsFolder;

      appsFolder = System.IO.Path.Combine(System.Environment.CurrentDirectory, EXTENSIONS_FOLDER);

      if (!System.IO.Directory.Exists(appsFolder))
      {
        System.IO.Directory.CreateDirectory(appsFolder);
      }

      return appsFolder;
    }

    /// <summary>
    /// Gets a sub-folder of the extensions folder.
    /// </summary>
    /// <param name="name">The name of your extension folder, relative to the extensions folder root.</param>
    /// <param name="create">Specifies whether to create the folder if it does not already exist.</param>
    /// <returns></returns>
    static public string GetExtensionFolder(string name, Boolean create)
		{
      string result = System.IO.Path.Combine(GetExtensionsFolder(), name);

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
    /// Gets the default application data storage folder location, used for logs and database files.
    /// </summary>
    /// <returns></returns>
    static public string GetDataFolder()
    {
      string result;

      result = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Nucleus");
      
      if (!System.IO.Directory.Exists(result))
      {
        System.IO.Directory.CreateDirectory(result);
      }

      return result;
    }

    /// <summary>
    /// Gets an application data storage folder sub-folder. 
    /// </summary>
    /// <param name="SubFolder"></param>
    /// <returns></returns>
    static public string GetDataFolder(string SubFolder)
    {
      string strResult = GetDataFolder();
      strResult = System.IO.Path.Combine(strResult, SubFolder);

      if (!System.IO.Directory.Exists(strResult))
        System.IO.Directory.CreateDirectory(strResult);

      return strResult;
    }

    //static public string GetDataFolder(string PluginFolder, string SubFolder)
    //{
    //  string strResult = GetDataFolder(Nucleus.Abstractions);
    //  strResult = System.IO.Path.Combine(System.IO.Path.Combine(strResult, PluginFolder), SubFolder);

    //  if (!System.IO.Directory.Exists(strResult))
    //    System.IO.Directory.CreateDirectory(strResult);

    //  return strResult;
    //}
  }
}
