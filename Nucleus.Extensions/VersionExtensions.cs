using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions
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
      if (compareToVersion == "*") compareToVersion = "*.*";
      System.Version compareTo = Version.Parse(compareToVersion.Replace("*", "0"));
			return ZeroUndefinedElements(version).CompareTo(ZeroUndefinedElements(compareTo)) < 0;
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
      if (compareToVersion == "*") compareToVersion = "*.*";
      System.Version compareTo = Version.Parse(compareToVersion.Replace("*", "65535"));
			return ZeroUndefinedElements(version).CompareTo(ZeroUndefinedElements(compareTo)) > 0;
		}

    /// <summary>
    /// Replace version elements which have been set to -1 with zero, so that comparisons work properly
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    static private Version ZeroUndefinedElements(System.Version version)
    {
      return new Version(version.Major, version.Minor==-1 ? 0 : version.Minor, version.Build==-1 ? 0 : version.Build, version.Revision==-1 ? 0 : version.Revision);
    }

		///// <summary>
		///// Return a string representation of the version with additional ".0"'s appended, if the specified version is missing any of the 
  //  /// "minor", "revision" or "minorrevision" parts.
		///// </summary>
		///// <param name="version"></param>
		///// <returns></returns>
		///// <remarks>
		///// This function is used to ensure that a parsed string version uses zero for the missing version number parts, because Version.Parse() 
  //  /// sets missing parts to -1 instead of zero, which breaks version comparison between two System.Version instances, when each Version instance
  //  /// has different parts specified/missing.
		///// </remarks>
		////static private string PadMissingVersionElements(string version)
		////{
  ////    // pad out version components because Version.Parse does not assume zero for missing parts
  ////    while (version.Split('c').Length < 3)
  ////    {
  ////      version += ".0";
  ////    }
      
		////	return version;
		////}

	}
}
