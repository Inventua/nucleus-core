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
	/// Html helper used to render a breadcrumb control.
	/// </summary>
	public static class BreadcrumbHtmlHelper
	{
		/// <summary>
		/// Renders an Breadcrumb control.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <returns></returns>
		public static IHtmlContent Breadcrumb(this IHtmlHelper htmlHelper)
		{
			return Nucleus.ViewFeatures.HtmlContent.Breadcrumb.Build(htmlHelper.ViewContext, null).Result;
		}

		/// <summary>
		/// Renders an Breadcrumb control.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent Breadcrumb(this IHtmlHelper htmlHelper, object htmlAttributes)
		{
			return Nucleus.ViewFeatures.HtmlContent.Breadcrumb.Build(htmlHelper.ViewContext, htmlAttributes).Result;
		}
	}
}
