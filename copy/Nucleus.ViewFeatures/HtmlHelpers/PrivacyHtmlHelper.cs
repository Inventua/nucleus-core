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
	public static class PrivacyHtmlHelper
	{
		/// <summary>
		/// Returns an anchor (<![CDATA[a]]>) element which links to the site's Privacy page.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="caption">The inner text of the anchor element.</param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		/// <remarks>
		/// If the site does not have a configured privacy page, nothing is rendered.
		/// </remarks>
		public static IHtmlContent Privacy(this IHtmlHelper htmlHelper, string caption, object htmlAttributes)
		{
			return Nucleus.ViewFeatures.HtmlContent.Privacy.Build(htmlHelper.ViewContext, caption, htmlAttributes);
		}
	}
}
