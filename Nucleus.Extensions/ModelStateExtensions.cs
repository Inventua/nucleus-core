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
    /// Add a model state error message for the specified property.
    /// </summary>
    /// <typeparam name="TType"></typeparam>
    /// <typeparam name="TModel"></typeparam>
    /// <param name="modelState"></param>
    /// <param name="expression"></param>
    /// <param name="message"></param>
    /// <exception cref="ArgumentException"></exception>
    public static void AddModelError<TType, TModel>(this Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, Expression<Func<TType, TModel>> expression, string message)
    {
      if (expression.Body is not MemberExpression memberExpression)
      {
        throw new ArgumentException("Expression must be a member expression.");
      }

      string name = memberExpression.Member.Name;

      modelState.AddModelError(name, message);
    }

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
    /// Remove an item from model state, specified by an expression. 
    /// </summary>
    /// <param name="modelState"></param>
    /// <param name="expression"></param>
    public static void Remove<TType, TProperty>(this Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, Expression<Func<TType, TProperty>> expression)
    {
      if (expression.Body is not MemberExpression memberExpression)
      {
        throw new ArgumentException("Expression must be a member expression.");
      }

      string name = memberExpression.Member.Name;

      modelState.Remove(name);
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
