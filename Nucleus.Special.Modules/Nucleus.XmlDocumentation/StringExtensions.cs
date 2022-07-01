using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.XmlDocumentation
{
	public static class StringExtensions
	{
		/// <summary>
		/// Removes the namespace from parameters string with full class names and returns the "simple" name for the parameters.  Multiple types may be separated by commas.
		/// </summary>
		/// <param name="value"></param>
		public static string GetSimpleParameterTypes(this string value)
		{
			List<string> results = new();
			if (String.IsNullOrEmpty(value)) return "";
			foreach (string parameter in value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string parameterType = parameter.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
				if (String.IsNullOrEmpty(parameterType)) return "";
				results.Add(parameterType.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault());
			}

			return String.Join(',', results);
		}

		/// <summary>
		/// Filter characters from descriptions in XML files so that we can use multiple lines, etc in XML but show
		/// the description HTML-style, without line breaks and multi-spaces/tabs.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string FilterXMLWhiteSpace(this string value)
		{
			int leadingSpaces;

			if (value == null) return null;

			// remove leading CRLF characters
			value = System.Text.RegularExpressions.Regex.Replace(value, "^([\\r\\n]*)", "", System.Text.RegularExpressions.RegexOptions.Multiline);

			leadingSpaces = CountLeadingSpaces(value);

			// remove leading white space at the start of lines
			value = value.Replace("\n" + new string(' ', leadingSpaces), "\n");

			// replace tabs with spaces
			value = System.Text.RegularExpressions.Regex.Replace(value, "(.*?)\\t", "  ", System.Text.RegularExpressions.RegexOptions.Multiline);

			// replace trailing white space at the end of lines with a single space
			value = System.Text.RegularExpressions.Regex.Replace(value, "$[\\t\\ ]*", " ", System.Text.RegularExpressions.RegexOptions.Multiline);

			// turn line feeds into CRLF
			value = System.Text.RegularExpressions.Regex.Replace(value, "(\\n)", "\r\n", System.Text.RegularExpressions.RegexOptions.Multiline);

			// remove extra spaces at the start and end
			value = value.Trim();

			return value;
		}

		private static int CountLeadingSpaces(string value)
		{
			return value.TakeWhile(chr => chr == ' ').Count();
		}
	}
}
