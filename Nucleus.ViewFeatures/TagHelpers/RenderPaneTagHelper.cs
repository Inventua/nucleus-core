using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Nucleus.Abstractions.Layout;
using Microsoft.AspNetCore.Html;

namespace Nucleus.ViewFeatures.TagHelpers
{
  /// <summary>
  /// Tag helper used to render the modules which are within the specified pane for the page being rendered.
  /// </summary>
  /// <remarks>
  /// The RenderPaneTagHelper is used by Layouts.
  /// </remarks>
  [HtmlTargetElement("RenderPane")]
	public class RenderPaneTagHelper : TagHelper
	{
		/// <summary>
		/// Provides access to view context.
		/// </summary>
		[ViewContext]
		[HtmlAttributeNotBound]
		public ViewContext ViewContext { get; set; }

    /// <summary>
    /// The name of the pane to render.
    /// </summary>
    [HtmlAttributeName("name")]
    public string Name { get; set; }

		/// <summary>
		/// Generate the output.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
      IModuleContentRenderer renderer = this.ViewContext.HttpContext.RequestServices.GetService<IModuleContentRenderer>();
      Context nucleusContext = this.ViewContext.HttpContext.RequestServices.GetService<Context>();
      
      IHtmlContent content = await renderer.RenderPaneAsync(this.ViewContext, nucleusContext, this.Name);
            
			if (content == null)
			{
				output.SuppressOutput();
			}
			else
			{
        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.PostContent.SetHtmlContent(content);
      }

			await Task.CompletedTask;
		}
	}
}