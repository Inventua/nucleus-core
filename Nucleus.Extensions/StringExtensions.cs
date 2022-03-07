using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Extension functions for numeric types.
	/// </summary>
	public static class StringExtensions
	{
		/// <summary>
		/// Encode a value for use in an Url.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <remarks>
		/// This function replaces non-alphanumeric characters with a dash and converts the value to lower case.  This is preferable to
		/// url-encoding the value, as it results in a "friendlier" url.
		/// </remarks>
		public static string FriendlyEncode(this string value)
		{
			const string UNSAFE_CHARACTERS = "[^a-zA-Z0-9]+";
			return System.Text.RegularExpressions.Regex.Replace(value, UNSAFE_CHARACTERS, "-").ToLower();
		}

	}

}
