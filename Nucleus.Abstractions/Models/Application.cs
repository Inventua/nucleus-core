using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Nucleus application information.
	/// </summary>
	public class Application
	{
		/// <summary>
		/// Initialize a new instance of the class.
		/// </summary>
		public Application()
		{
			this.IsInstalled = System.IO.File.Exists(GetInstallLogPath()) && System.IO.Directory.Exists(Configuration.FolderOptions.GetExtensionsFolderStatic(false));
		}

		/// <summary>
		/// Get whether the application has been installed.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// Checks whether the /Extensions folder exists, and a file named /Setup/install-log.config exists.
		/// </remarks>
		public Boolean IsInstalled { get; private set;}
		
    /// <summary>
    /// Uri used to access the admin control panel.
    /// </summary>
    public string ControlPanelUri { get; private set; } = "~/Admin/Index/Index";

    private string GetInstallLogPath()
		{
			return System.IO.Path.Combine(Environment.CurrentDirectory, "Setup", "install-log.config"); 
		}

    /// <summary>
    /// Set the base Uri for the control panel.
    /// </summary>
    /// <param name="uri"></param>
    /// <remarks>
    /// Set to an empty string to suppress the admin UI.  Calling with a uri value or null does not change the control panel Uri (uses the default).
    /// </remarks>
    public void SetControlPanelUri(string uri)
    {
      if (uri != null)
      {
        this.ControlPanelUri = uri;
      }
    }

    /// <summary>
    /// Records a log file to indicate that Nucleus is installed (the setup wizard has run).
    /// </summary>
    public void SetInstalled()
		{
			string version;

			AssemblyInformationalVersionAttribute attr = Assembly.GetCallingAssembly().GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>();
			version =  (attr == null ? String.Empty : attr.InformationalVersion);

			System.IO.File.WriteAllText(GetInstallLogPath(),
				System.Text.Json.JsonSerializer.Serialize(new
				{
					InstallDate = DateTime.UtcNow,
					InstallVersion = version
				}));

      this.IsInstalled = true;
		}
	}
}
