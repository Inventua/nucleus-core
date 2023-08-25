using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.ViewFeatures;

namespace Nucleus.Modules.IFrame.HtmlHelpers
{
  /// <summary>
  /// Html helper used to render a (<![CDATA[IFRAME]]>) element.
  /// </summary>
  public static class FrameHtmlHelper
  {
    /// <summary>
    /// Render an (<![CDATA[IFRAME]]>) element with the specified attributes.
    /// </summary>
    /// <param name="htmlHelper"></param>
    /// <param name="url"></param>
    /// <param name="htmlAttributes"></param>
    /// <returns></returns>
    public static IHtmlContent Frame(this IHtmlHelper htmlHelper, string url, object htmlAttributes)
    {
      TagBuilder outputBuilder = new("iframe");
      outputBuilder.Attributes.Add("src", url);

      outputBuilder.MergeAttributes(htmlAttributes);

      return outputBuilder;
    }
  }
}
