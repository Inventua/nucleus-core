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
	/// Html helper used to render an account control.
	/// </summary>
	public static class AccountHtmlHelper
	{
    /// <summary>
    /// Renders an account control.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="htmlAttributes"></param>
    /// <returns></returns>
    public static async Task<IHtmlContent> Account(this IHtmlHelper htmlHelper, object htmlAttributes)
    {
      return await Nucleus.ViewFeatures.HtmlContent.Account.Build(htmlHelper.ViewContext, "", htmlAttributes);
    }

    /// <summary>
    /// Renders an account control.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="buttonClass">
    /// If a non-blank value is specified, the value is appended to the css class for buttons within the account control, and the default 
    /// "btn-secondary" class is not applied.
    /// </param>
    /// <param name="htmlAttributes"></param>
    /// <returns></returns>
    public static async Task<IHtmlContent> Account(this IHtmlHelper htmlHelper, string buttonClass, object htmlAttributes)
		{			
			return await Nucleus.ViewFeatures.HtmlContent.Account.Build(htmlHelper.ViewContext, buttonClass, htmlAttributes);
		}
	}
}
