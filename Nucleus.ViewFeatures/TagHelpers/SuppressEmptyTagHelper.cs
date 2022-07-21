using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

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
	/// </remarks>
	[HtmlTargetElement(HtmlTargetElementAttribute.ElementCatchAllTarget, Attributes = "[suppress-empty=true]")]
	public class SuppressEmptyTagHelper : TagHelper
	{
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
			if (this.SuppressEmpty)
			{
				TagHelperContent content = await output.GetChildContentAsync();

				if (content.IsEmptyOrWhiteSpace)
				{
					output.SuppressOutput();
				}
			}

			await Task.CompletedTask;
		}
	}
}
