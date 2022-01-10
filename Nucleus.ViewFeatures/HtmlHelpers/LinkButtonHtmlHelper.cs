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
	/// <summary>
	/// Html helper used to render a link (<![CDATA[A]]>) element with a label and glyph which is styled as a button.
	/// </summary>
	public static class LinkButtonHtmlHelper
	{
		/// <summary>
		/// Returns a link (<![CDATA[A]]>) element with a label and glyph.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="glyph"></param>
		/// <param name="caption"></param>
		/// <param name="href"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent LinkButton(this IHtmlHelper htmlHelper, string glyph, string caption, string href, object htmlAttributes)
		{
			return Nucleus.ViewFeatures.HtmlContent.LinkButton.Build(htmlHelper.ViewContext, glyph, caption, href, true, htmlAttributes);
		}

		/// <summary>
		/// Returns a link (<![CDATA[A]]>) element with a label and glyph.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="glyph"></param>
		/// <param name="caption"></param>
		/// <param name="href"></param>
		/// <param name="buttonStyle"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent LinkButton(this IHtmlHelper htmlHelper, string glyph, string caption, string href, Boolean buttonStyle, object htmlAttributes)
		{
			return Nucleus.ViewFeatures.HtmlContent.LinkButton.Build(htmlHelper.ViewContext, glyph, caption, href, buttonStyle, htmlAttributes);
		}

	}
}
