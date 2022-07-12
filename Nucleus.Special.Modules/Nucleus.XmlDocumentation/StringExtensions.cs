using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.XmlDocumentation
{
	public static class StringExtensions
	{
		/////// <summary>
		/////// Removes the namespace from parameters string with full class names and returns the "simple" name for the parameters.  Multiple types may be separated by commas.
		/////// </summary>
		/////// <param name="value"></param>
		////public static string GetSimpleParameterTypes(this string value)
		////{
		////	List<string> results = new();
		////	if (String.IsNullOrEmpty(value)) return "";
		////	foreach (string parameter in value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
		////	{
		////		string parameterType = parameter.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
		////		if (String.IsNullOrEmpty(parameterType)) return "";

		////		if (parameterType.Contains('<') && parameterType.Contains('>'))
		////		{
		////			System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(parameterType, "(?<type1>[^<]*)<(?<type2>[^>]*)>.*");
		////			if (match.Success && match.Groups.Count == 3)
		////			{
		////				results.Add($"{GetSimpleParameterType(match.Groups[1].Value)}<{GetSimpleParameterType(match.Groups[2].Value)}>");
		////			}
		////			else
		////			{
		////				results.Add(parameter);
		////			}
		////		}
		////		else
		////		{
		////			results.Add(GetSimpleParameterType(parameterType));
		////		}
		////	}

		////	return String.Join(',', results);
		////}

		////private static string GetSimpleParameterType(string parameterType)
		////{
		////	return parameterType.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
		////}

		/// <summary>
		/// Filter characters from descriptions in XML files so that we can use multiple lines, etc in XML but show
		/// the description HTML-style, without line breaks and multi-spaces/tabs.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string FilterXMLWhiteSpace(this string value)
		{
			int leadingSpaces = 0;

			if (value == null) return null;

			// remove leading CRLF characters
			value = System.Text.RegularExpressions.Regex.Replace(value, "^([\\r\\n]*)", "", System.Text.RegularExpressions.RegexOptions.Multiline);

			// count the leading spaces in the first line if it is not a comment, or the first line that isn't a comment so that we can remove
			// leading spaces from each line so that they render properly in a <code> block.
			foreach (string line in value.Split("\n"))
			{
				if (!line.StartsWith('/'))
				{
					leadingSpaces = CountLeadingSpaces(line);
					break;
				}
			}

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
