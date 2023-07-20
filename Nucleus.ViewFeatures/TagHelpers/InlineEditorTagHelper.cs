using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Extensions.Authorization;
using Nucleus.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Nucleus.ViewFeatures.TagHelpers
{
  /// <summary>
  /// Controls rendering of a data-inline-edit-route attribute on an element, depending on whether the user is in 
  /// content edit mode.  Client-side components use the data-inline-edit-route attribute to handle inline editing,
  /// </summary>
  [HtmlTargetElement(HtmlTargetElementAttribute.ElementCatchAllTarget, Attributes = "[inline-edit-route]")]
	public class InlineEditorTagHelper : TagHelper
	{
    /// <summary>
		/// Provides access to view context.
		/// </summary>
		[ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; }

    /// <summary>
    /// Specifies whether to suppress an empty element.
    /// </summary>
    [HtmlAttributeName("inline-edit-route")]
		public string InlineEditRoute { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
      Context nucleusContext = this.ViewContext.HttpContext.RequestServices.GetService<Context>();
      
      if (!String.IsNullOrEmpty(this.InlineEditRoute) && this.ViewContext.HttpContext.User.IsEditing(this.ViewContext.HttpContext, nucleusContext.Site, nucleusContext.Page, nucleusContext.Module))
			{
        output.Attributes.Add("data-inline-edit-route", this.InlineEditRoute);        
      }

      await Task.CompletedTask;
		}
	}
}
