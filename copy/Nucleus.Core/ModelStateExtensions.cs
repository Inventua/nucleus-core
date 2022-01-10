using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions
{
	public static class ModelStateExtensions
	{
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
