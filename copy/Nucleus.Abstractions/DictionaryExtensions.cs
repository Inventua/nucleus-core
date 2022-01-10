using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions
{
	public static class DictionaryExtensions
	{
		/// <summary>
		/// Gets the specified value or returns an empty string if the key is not present.
		/// </summary>
		/// <param name="dict"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public static string TryGetValue(this Dictionary<string, string> dict, string key)
		{
			string value;
			return dict.TryGetValue(key, out value) ? value : "";
		}

		/// <summary>
		/// Adds or replaces the value for the specified key.
		/// </summary>
		/// <param name="dict"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void TrySetValue(this Dictionary<string, string> dict, string key, string value)
		{
			if (dict.ContainsKey(key))
			{
				dict[key] = value;
			}
			else
			{
				dict.Add(key, value);
			}
		}

		/// <summary>
		/// Adds or replaces the value for the specified key.
		/// </summary>
		/// <param name="dict"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void TrySetValue(this Dictionary<string, string> dict, string key, Boolean? value)
		{
			TrySetValue(dict, key, value.ToString());
		}

		/// <summary>
		/// Adds or replaces the value for the specified key.
		/// </summary>
		/// <param name="dict"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void TrySetValue(this Dictionary<string, string> dict, string key, int? value)
		{
			TrySetValue(dict, key, value.ToString());
		}

		/// <summary>
		/// Adds or replaces the value for the specified key.
		/// </summary>
		/// <param name="dict"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void TrySetValue(this Dictionary<string, string> dict, string key, Guid? value)
		{
			TrySetValue(dict, key, value.ToString());
		}
	}

}
