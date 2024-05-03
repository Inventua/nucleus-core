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
    /// Relative uri for the control panel UI entry point.
    /// </summary>
    public string Uri { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name"></param>
    /// <param name="uri"></param>
		public ControlPanelAttribute(string name, string uri) 
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException("Name can not be empty", nameof(name));
			}
      if (string.IsNullOrEmpty(uri))
      {
        throw new ArgumentException("Uri can not be empty", nameof(uri));
      }

      this.Name = name;
      this.Uri = uri;
    }

  }
}
