using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text;
using System.IO;

namespace Nucleus.Data.Common
{
	/// <summary>
	/// The DatabaseSchemaScript class represents database upgrade script meta-data and content.
	/// </summary>
	public class DatabaseSchemaScript
	{
		/// <summary>
		/// Script name (resource/file name)
		/// </summary>
		public string FullName { get; }

		/// <summary>
		/// Script contents (sql commands)
		/// </summary>
		public string Content { get; }

		/// <summary>
		/// Script schema version.  This is extracted from the filename.
		/// </summary>
		/// <remarks>
		/// Script file names should consist of a three-part version number, with Major.Minor.Build separated by dots.
		/// </remarks>
		public System.Version Version { get; }
				
		/// <summary>
		/// Represents a database schema update script.
		/// </summary>
		/// <param name="fullName">Script file name</param>
		/// <param name="version"></param>
		/// <param name="content"></param>
		public DatabaseSchemaScript(string fullName, System.Version version, string content)
		{
			this.FullName = fullName;
			this.Version = version;
			this.Content = content;
		}

		/// <summary>
		/// Create a DatabaseSchemaScript object representing an embedded database script.
		/// </summary>
		/// <param name="scriptsAssembly"></param>
		/// <param name="scriptNamespace"></param>
		/// <param name="resourceName">Relative name of an embedded database script within the scripts namespace.</param>
		/// <returns>A DatabaseSchemaScript object</returns>
		public static DatabaseSchemaScript BuildManifestSchemaScript(System.Reflection.Assembly scriptsAssembly, string scriptNamespace, string resourceName)
		{			
			StreamReader reader = new(scriptsAssembly.GetManifestResourceStream(resourceName));
			string shortResourceName = resourceName.Replace(scriptNamespace, "", StringComparison.OrdinalIgnoreCase);
			return new DatabaseSchemaScript
				(
					resourceName,
					ParseVersion(shortResourceName),
					reader.ReadToEnd()
				);
		}

		/// <summary>
		/// Determine whether the filename of an embedded resource file is a version number.
		/// </summary>
		/// <param name="scriptNamespace"></param>
		/// <param name="resourceName">Name of an embedded database script.</param>
		public static Boolean CanParseVersion(string scriptNamespace, string resourceName)
		{
			string shortResourceName = resourceName.Replace(scriptNamespace, "", StringComparison.OrdinalIgnoreCase);
			return System.Version.TryParse(System.IO.Path.GetFileNameWithoutExtension(shortResourceName), out Version result);
		}

		/// <summary>
		/// Parse the filename of an embedded resource file and return its version number.
		/// </summary>
		/// <param name="resourceName">Name of an embedded database script.</param>
		/// <returns>System.Version containing the embedded script file version.  The version is the file name without the file extension.</returns>
		private static System.Version ParseVersion(string resourceName)
		{
			if (System.Version.TryParse(System.IO.Path.GetFileNameWithoutExtension(resourceName), out Version result))
			{
				return result;
			}
			else
			{
				throw new InvalidOperationException($"Unexpected resource {resourceName} in dataprovider/scripts.");
			}
		}
	}
}
