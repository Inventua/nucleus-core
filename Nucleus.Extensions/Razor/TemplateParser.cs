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
		/// <returns></returns>
		public static string ParseTemplate<T>(this string template, T model)
			where T : MailTemplateModelBase<T>
		{
			string result;

			// Parse the template as Razor 
			result = RazorParser.Parse<T>(template, model);

			// Parse the template as "simple"
			SimpleParser.Parse(result, model);
			
			return result;
		}
	}
}
