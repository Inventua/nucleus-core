using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions
{
	/// <summary>
	/// Extensions for the System.Version class.
	/// </summary>
	static public class VersionExtensions
	{
		/// <summary>
		/// Returns a valid indicating whether the specified string represents a version which is less than this version.
		/// </summary>
		/// <param name="version"></param>
		/// <param name="compareToVersion"></param>
		/// <returns></returns>
		/// <remarks>
		/// compareToVersion should contain a string in the form of a version, but can contain a '*' in place of any of the version fields.
		/// </remarks>
		static public Boolean IsLessThan(this System.Version version, string compareToVersion)
		{			
			System.Version compareTo = Version.Parse(CheckMinorVersion(compareToVersion).Replace("*", "0"));
			return version.CompareTo(compareTo) < 0;
		}

		/// <summary>
		/// Returns a valid indicating whether the specified string represents a version which is greater than this version.
		/// </summary>
		/// <param name="version"></param>
		/// <param name="compareToVersion"></param>
		/// <returns></returns>
		/// <remarks>
		/// compareToVersion should contain a string in the form of a version, but can contain a '*' in place of any of the version fields.
		/// </remarks>
		static public Boolean IsGreaterThan(this System.Version version, string compareToVersion)
		{
			if (!compareToVersion.Contains('.'))
			{
				compareToVersion += ".*";
			}

			System.Version compareTo = Version.Parse(CheckMinorVersion(compareToVersion).Replace("*", "65535"));
			return version.CompareTo(compareTo) > 0;
		}

		/// <summary>
		/// Return a string representation of the version with '.0' appended, if the specified version contains only a number ('major' version).
		/// </summary>
		/// <param name="version"></param>
		/// <returns></returns>
		/// <remarks>
		/// This function is used to ensure that a string version has at least a major.minor version number, so that it can be
		/// parsed by Version.Parse().
		/// </remarks>
		static private string CheckMinorVersion(string version)
		{
			if (!version.Contains('.'))
			{
				version += ".0";
			}
			return version;
		}

	}
}
