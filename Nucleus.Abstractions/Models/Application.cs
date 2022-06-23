using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

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
		public Boolean IsInstalled { get; }
		

		private string GetInstallLogPath()
		{
			return System.IO.Path.Combine(Environment.CurrentDirectory, "Setup", "install-log.config"); 
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
		}
	}
}
