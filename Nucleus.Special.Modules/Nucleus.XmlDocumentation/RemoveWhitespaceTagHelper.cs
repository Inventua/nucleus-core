using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Nucleus.XmlDocumentation
{
	/// <summary>
	/// Removes unwanted white space from the output.
	/// </summary>	
	/// <remarks>
	/// The _RenderMixedContent razor view loops through elements in a <see cref="Models.Serialization.MixedContent"/> and renders
	/// each item type as needed, but the Razor parser retains white space (CRLFs and tabs) between each mixed content item, which 
	/// manifests as (visible) white space in the output.  This tag helper removes the unwanted space between each item.  A 
	/// similar (but different) function is performed by <see cref="DocumentationParser.TrimStrings"/>, which removes unwanted white
	/// space from XmlText elements in mixed content that results from the way that MSBuild outputs XML comments files.
	/// </remarks>

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