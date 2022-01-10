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
	/// Html helper used to render a menu.
	/// </summary>
	public static class MenuHtmlHelper
	{
		/// <summary>
		/// Returns a list (<![CDATA[ul]]>) element containing the site's menu structure.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="maxLevels"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent Menu(this IHtmlHelper htmlHelper, int maxLevels, object htmlAttributes)
		{
			return Nucleus.ViewFeatures.HtmlContent.Menu.Build(htmlHelper.ViewContext, HtmlContent.Menu.MenuStyles.DropDown, maxLevels, htmlAttributes).Result;
		}

		/// <summary>
		/// Returns a list (<![CDATA[ul]]>) element containing the site's menu structure.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="menuStyle"></param>
		/// <param name="maxLevels"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent Menu(this IHtmlHelper htmlHelper, HtmlContent.Menu.MenuStyles menuStyle, int maxLevels, object htmlAttributes)
		{
			return Nucleus.ViewFeatures.HtmlContent.Menu.Build(htmlHelper.ViewContext, menuStyle, maxLevels, htmlAttributes).Result;
		}
	}
}
