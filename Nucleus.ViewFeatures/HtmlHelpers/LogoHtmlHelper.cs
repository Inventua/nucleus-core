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
	/// Html helper used to render the site's logo.
	/// </summary>
	public static class LogoHtmlHelper
	{
		/// <summary>
		/// Returns an anchor (<![CDATA[a]]>) element which links to the site's home page, with an image inside which renders the site logo.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		/// <remarks>
		/// If the site does not have a configured logo, nothing is rendered.
		/// </remarks>
		public static IHtmlContent Logo(this IHtmlHelper htmlHelper, object htmlAttributes)
		{
			return Nucleus.ViewFeatures.HtmlContent.Logo.Build(htmlHelper.ViewContext, "", true, htmlAttributes).Result;
		}

    /// <summary>
		/// Returns an anchor (<![CDATA[a]]>) element which links to the site's home page, with an image inside which renders the site logo.
		/// </summary>
		/// <param name="htmlHelper"></param>
    /// <param name="fallbackToSiteTitle"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		/// <remarks>
		/// If the site does not have a configured logo, nothing is rendered.
		/// </remarks>
		public static IHtmlContent Logo(this IHtmlHelper htmlHelper, Boolean fallbackToSiteTitle, object htmlAttributes)
    {
      return Nucleus.ViewFeatures.HtmlContent.Logo.Build(htmlHelper.ViewContext, "", fallbackToSiteTitle, htmlAttributes).Result;
    }

    /// <summary>
    /// Returns an anchor (<![CDATA[a]]>) element which links to the site's home page, with an image inside which renders the site logo.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="caption">The alt text of the image element. </param>
    /// <param name="htmlAttributes"></param>
    /// <returns></returns>
    /// <remarks>
    /// If the site does not have a configured logo, nothing is rendered.
    /// </remarks>
    public static IHtmlContent Logo(this IHtmlHelper htmlHelper, string caption, object htmlAttributes)
		{
			return Nucleus.ViewFeatures.HtmlContent.Logo.Build(htmlHelper.ViewContext, caption, true, htmlAttributes).Result;
		}

    /// <summary>
    /// Returns an anchor (<![CDATA[a]]>) element which links to the site's home page, with an image inside which renders the site logo.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="caption">The alt text of the image element. </param>
    /// <param name="fallbackToSiteTitle"></param>
    /// <param name="htmlAttributes"></param>
    /// <returns></returns>
    /// <remarks>
    /// If the site does not have a configured logo, nothing is rendered.
    /// </remarks>
    public static IHtmlContent Logo(this IHtmlHelper htmlHelper, string caption, Boolean fallbackToSiteTitle, object htmlAttributes)
    {
      return Nucleus.ViewFeatures.HtmlContent.Logo.Build(htmlHelper.ViewContext, caption, fallbackToSiteTitle, htmlAttributes).Result;
    }
  }
}
