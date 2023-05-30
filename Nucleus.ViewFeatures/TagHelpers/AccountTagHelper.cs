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
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Mvc.TagHelpers;

namespace Nucleus.ViewFeatures.TagHelpers
{
	/// <summary>
	/// Renders an account control.
	/// </summary>
	[HtmlTargetElement("Account")]
	public class AccountTagHelper : TagHelper
	{		
		/// <summary>
		/// Provides access to view context.
		/// </summary>
		[ViewContext]
		[HtmlAttributeNotBound]
		public ViewContext ViewContext { get; set; }
			
    /// <summary>
    /// Css classes to apply to the dropdown button.
    /// </summary>
    /// <remarks>
    /// If a non-blank value is specified, the value is appended to the css class for buttons within the account control, and the default "btn-secondary" class 
    /// is not applied.
    /// </remarks>
    [HtmlAttributeName("button-class")]
    public string ButtonClass { get; set; }

		/// <summary>
		/// Generate the output.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			TagBuilder builder = await Nucleus.ViewFeatures.HtmlContent.Account.Build(this.ViewContext, this.ButtonClass, null);

			output.TagName = builder.TagName;
			output.TagMode = TagMode.StartTagAndEndTag;
			output.MergeAttributes(builder);
			output.Content.AppendHtml(builder.InnerHtml);

			await Task.CompletedTask;
		}
	}
}