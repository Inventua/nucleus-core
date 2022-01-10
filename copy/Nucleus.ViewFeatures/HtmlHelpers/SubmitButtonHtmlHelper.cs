using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace Nucleus.ViewFeatures.HtmlHelpers
{
	public static class SubmitButtonHtmlHelper
	{
		/// <summary>
		/// Returns an (<![CDATA[input]]>) element with a label and glyph.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent SubmitButton(this IHtmlHelper htmlHelper, string glyph, string caption, string formaction, object htmlAttributes)
		{
			return Nucleus.ViewFeatures.HtmlContent.SubmitButton.Build(htmlHelper.ViewContext, glyph, caption, formaction, htmlAttributes);
		}
	}
}
