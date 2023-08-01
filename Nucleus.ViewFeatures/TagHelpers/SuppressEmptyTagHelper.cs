using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Nucleus.Extensions.Authorization;

namespace Nucleus.ViewFeatures.TagHelpers
{
  /// <summary>
  /// Prevent rendering of the element when it has no content.
  /// </summary>
  /// <remarks>
  /// The purpose of this tag helper is for Nucleus layouts.  Some layouts have many Panes which present content in 
  /// various ways, of which only a few are used on any given page.  By adding a suppress-empty="true" attribute to 
  /// the element which wraps the Pane, the element can be completely suppressed when not in use. 
  /// This tag helper operates on any HTML element with a suppress-empty attribute with a value of true.  
  /// The tag helper does not suppress empty panes if the user is in edit mode.  This is so that empty containers 
  /// are available as drop targets for the inline drag/drop "move module" functionality.
  /// </remarks>
  /// <example>
  /// <div class="GridPane" suppress-empty="true"></div>
  /// </example>
  [HtmlTargetElement(HtmlTargetElementAttribute.ElementCatchAllTarget, Attributes = "[suppress-empty=true]")]
	public class SuppressEmptyTagHelper : TagHelper
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
    [HtmlAttributeName("suppress-empty")]
		public Boolean SuppressEmpty { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
      // only suppress empty panes if the user is not in edit mode.
			if (this.SuppressEmpty && !this.ViewContext.HttpContext.User.IsEditing(this.ViewContext.HttpContext))
			{
				TagHelperContent content = await output.GetChildContentAsync();

        if (content.IsEmptyOrWhiteSpace && output.PreContent.IsEmptyOrWhiteSpace && output.Content.IsEmptyOrWhiteSpace && output.PostContent.IsEmptyOrWhiteSpace && output.PreElement.IsEmptyOrWhiteSpace && output.PostElement.IsEmptyOrWhiteSpace)
        {
					output.SuppressOutput();
				}
			}

			await Task.CompletedTask;
		}
	}
}
