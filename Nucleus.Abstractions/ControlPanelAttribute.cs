using System;
using System.Reflection.Metadata;

namespace Nucleus.Abstractions
{
  /// <summary>
  /// Attribute used to specify values for a Nucleus control panel implementation.
  /// </summary>
  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	public class ControlPanelAttribute : Attribute
	{
    /// <summary>
    /// Name of the control panel implementation.  Can be used to select an admin UI in app settings.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Relative root path to use for embedded resources.  Should start with "/".
    /// </summary>
    public string ResourcesRootPath { get; }

    /// <summary>
    /// Relative uri for the control panel UI entry point.
    /// </summary>
    public string Uri { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name"></param>
    /// <param name="resourcesRootPath"></param>
    /// <param name="uri"></param>
		public ControlPanelAttribute(string name, string resourcesRootPath, string uri) 
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException("Name can not be empty", nameof(name));
			}
      if (string.IsNullOrEmpty(resourcesRootPath))
      {
        throw new ArgumentException("resourcesRootPath can not be empty", nameof(resourcesRootPath));
      }
      if (string.IsNullOrEmpty(uri))
      {
        throw new ArgumentException("Uri can not be empty", nameof(uri));
      }

      this.Name = name;
      this.ResourcesRootPath = resourcesRootPath;
      this.Uri = uri;
    }

  }
}
