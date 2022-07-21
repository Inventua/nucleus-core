using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Mail;

namespace Nucleus.Extensions.Razor
{
	/// <summary>
	/// Parse a template string using both the Razor parser and the Simple parser.
	/// </summary>
	public static class TemplateParser
	{
		/// <summary>
		/// Parse a template string.
		/// </summary>
		/// <param name="template"></param>
		/// <param name="model"></param>
		/// <typeparam name="TModel">Type of the model object being sent to the Razor engine.</typeparam>
		/// <returns></returns>
		public static async Task<string> ParseTemplate<TModel>(this string template, TModel model)
			where TModel : class
		{
			string result;

			// Parse the template as Razor 
			result = await RazorParser.Parse<TModel>(template, model);

			// Parse the template as "simple"
			result = SimpleParser.Parse(result, model);

			return result;
		}
	}
}
