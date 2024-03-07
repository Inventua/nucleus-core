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
	public static class UploadButtonHtmlHelper
	{
    /// <summary>
		/// Returns an upload button with a label and glyph.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="caption"></param>
		/// <param name="formaction"></param>
    /// <param name="dataTarget"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent UploadButton(this IHtmlHelper htmlHelper, string caption, string formaction, string dataTarget, object htmlAttributes = null)
    {
      return Nucleus.ViewFeatures.HtmlContent.UploadButton.Build(htmlHelper.ViewContext, "&#xe147", caption, formaction, dataTarget, true, htmlAttributes);
    }

    /// <summary>
    /// Returns an upload button with a label and glyph.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="glyph"></param>
    /// <param name="caption"></param>
    /// <param name="formaction"></param>
    /// <param name="dataTarget"></param>
    /// <param name="htmlAttributes"></param>
    /// <returns></returns>
    public static IHtmlContent UploadButton(this IHtmlHelper htmlHelper, string glyph, string caption, string formaction, string dataTarget, object htmlAttributes = null)
		{
			return Nucleus.ViewFeatures.HtmlContent.UploadButton.Build(htmlHelper.ViewContext, glyph, caption, formaction, dataTarget, true, htmlAttributes);
		}

    /// <summary>
    /// Returns a upload button with a label and glyph.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="glyph"></param>
    /// <param name="caption"></param>
    /// <param name="formaction"></param>
    /// <param name="dataTarget"></param>
    /// <param name="buttonStyle"></param>
    /// <param name="htmlAttributes"></param>
    /// <returns></returns>
    public static IHtmlContent UploadButton(this IHtmlHelper htmlHelper, string glyph, string caption, string formaction, string dataTarget, Boolean buttonStyle, object htmlAttributes = null)
		{
			return Nucleus.ViewFeatures.HtmlContent.UploadButton.Build(htmlHelper.ViewContext, glyph, caption, formaction, dataTarget, buttonStyle, htmlAttributes);
		}

	}
}
