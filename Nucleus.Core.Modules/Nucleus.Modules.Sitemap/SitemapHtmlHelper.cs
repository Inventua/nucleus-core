using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nucleus.Modules.Sitemap
{
	public static class SitemapHtmlHelper
	{
		/// <summary>
		/// Renders a site map.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent Sitemap(this IHtmlHelper htmlHelper, Nucleus.Modules.Sitemap.RootPageTypes rootPageType, Guid selectedPageId, Boolean showDescription, int levels, Directions direction, object htmlAttributes)
		{
			return SitemapRenderer.Build(htmlHelper.ViewContext, rootPageType, selectedPageId, showDescription, levels, direction, htmlAttributes).Result;
		}
	}
}
