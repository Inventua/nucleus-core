using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Nucleus.ViewFeatures.TagHelpers
{
	/// <summary>
	/// Removes unwanted white space from the output.
	/// </summary>	
	[HtmlTargetElement("RemoveWhitespace")]
	public class RemoveWhitespaceTagHelper : TagHelper
	{
		/// <summary>
		/// Generate the output.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			TagHelperContent content = await output.GetChildContentAsync();

			output.SuppressOutput();
			output.Content.AppendHtml(content.GetContent().Replace("\r\n", "").Replace("\t", ""));
		}
	}
}