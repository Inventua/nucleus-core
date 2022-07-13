using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Nucleus.XmlDocumentation
{
	/// <summary>
	/// Returns an anchor (<![CDATA[a]]>) element which links to the site's Terms page.
	/// </summary>	
	/// <remarks>
	/// If the site does not have a configured terms page, nothing is rendered.
	/// </remarks>

	[HtmlTargetElement("RemoveWhitespace")]
	public class RemoveWhitespaceTagHelper : TagHelper
	{
		/// <summary>
		/// Provides access to view context.
		/// </summary>
		[ViewContext]
		[HtmlAttributeNotBound]
		public ViewContext ViewContext { get; set; }

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