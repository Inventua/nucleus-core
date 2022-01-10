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
		/// Encode a value for use in an Url, but replace non-alphanumeric characters with a dash instead of url-encoded values.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string FriendlyEncode(this string value)
		{
			const string UNSAFE_CHARACTERS = "[^a-zA-Z0-9]+";
			return System.Text.RegularExpressions.Regex.Replace(value, UNSAFE_CHARACTERS, "-");
		}

	}

}
