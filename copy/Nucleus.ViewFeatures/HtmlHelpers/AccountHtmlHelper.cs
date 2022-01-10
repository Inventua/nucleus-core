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
	public static class AccountHtmlHelper
	{
		/// <summary>
		/// Renders an account control.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent Account(this IHtmlHelper htmlHelper, object htmlAttributes)
		{			
			return Nucleus.ViewFeatures.HtmlContent.Account.Build(htmlHelper.ViewContext, htmlAttributes);
		}
	}
}
