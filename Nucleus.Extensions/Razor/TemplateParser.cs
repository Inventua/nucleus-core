using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		public static string ParseTemplate<TRazorTemplateBase>(this string template, Object model)
			where TRazorTemplateBase : MailRazorTemplateBase
		{
			string result;

			// Parse the template as Razor 
			result = RazorParser.Parse<TRazorTemplateBase>(template, model);

			// Parse the template as "simple"
			SimpleParser.Parse(result, model);
			
			return result;
		}
	}
}
