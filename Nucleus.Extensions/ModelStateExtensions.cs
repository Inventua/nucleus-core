using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Extensions used to read and manipulate a <seealso cref="Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary"/>.
	/// </summary>
	public static class ModelStateExtensions
	{
		/// <summary>
		/// Remove all items with the specifed prefix from model state
		/// </summary>
		/// <param name="modelState"></param>
		/// <param name="prefix"></param>
		public static void RemovePrefix(this Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, string prefix)
		{
			foreach (KeyValuePair<string, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry> item in modelState.FindKeysWithPrefix(prefix))
			{
				modelState.Remove(item.Key);
			}
		}

		/// <summary>
		/// Convert a ModelStateDictionary to a comma-separated string, suitable for on-screen display.
		/// </summary>
		/// <param name="modelState"></param>
		/// <returns></returns>
		public static string ToErrorString(this Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
		{
			System.Text.StringBuilder result = new();

			if (!modelState.IsValid)
			{
				foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry entry in modelState.Values)
				{
					foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error in entry.Errors)
					{
						if (result.Length > 0)
						{
							result.Append(" ,");
						}

						result.Append(error.ErrorMessage);
					}
				}
			}

			return result.ToString();
		}
	}
}
