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
  /// Html helper used to render a module title.
  /// </summary>
  /// <remarks>
  /// Renders the module title.  If the user has module edit rights and is editing page content, render attributes
  /// to enable inline editing of the module title.
  /// </remarks>
  public static class ModuleTitleHtmlHelper
	{
    /// <summary>
    /// Renders a header element with its content set to the module title.  If the user has module edit rights 
    /// and is editing page content, render attributes to enable inline editing of the module title.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <returns></returns>
    public static IHtmlContent ModuleTitle(this IHtmlHelper htmlHelper)
    {
      return Nucleus.ViewFeatures.HtmlContent.ModuleTitle.Build(htmlHelper.ViewContext, "", null);
    }

    /// <summary>
    /// Renders a header element with its content set to the module title.  If the user has module edit rights 
    /// and is editing page content, render attributes to enable inline editing of the module title.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="tag"></param>
    /// <param name="htmlAttributes"></param>
    /// <returns></returns>
    public static IHtmlContent ModuleTitle(this IHtmlHelper htmlHelper, string tag, object htmlAttributes)
		{
			return Nucleus.ViewFeatures.HtmlContent.ModuleTitle.Build(htmlHelper.ViewContext, tag, htmlAttributes);
		}

    /// <summary>
    /// Renders a header element with its content set to the module title.  If the user has module edit rights 
    /// and is editing page content, render attributes to enable inline editing of the module title.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="htmlAttributes"></param>
    /// <returns></returns>
    public static IHtmlContent ModuleTitle(this IHtmlHelper htmlHelper, object htmlAttributes)
    {
      return Nucleus.ViewFeatures.HtmlContent.ModuleTitle.Build(htmlHelper.ViewContext, "", htmlAttributes);
    }

  }
}
