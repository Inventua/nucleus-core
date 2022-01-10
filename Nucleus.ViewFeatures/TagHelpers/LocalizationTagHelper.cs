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
	/// Localization tag helper.
	/// </summary>
	/// <remarks>
	/// This function is not complete.
	/// The <see cref="LocalizationTagHelper" /> replaces the tag content with localized data, using the <see cref="ResourceId"/> to 
	/// look up localized content.
	/// </remarks>
	[HtmlTargetElement(Attributes = "resource-id")]
	public class LocalizationTagHelper : TagHelper
	{
		/// <summary>
		/// Provides access to view context.
		/// </summary>
		[ViewContext]
		[HtmlAttributeNotBound]
		public ViewContext ViewContext { get; set; }

		/// <summary>
		/// Localization resource id
		/// </summary>
		public string ResourceId { get; set; }

		/// <summary>
		/// Replace the output with a localized value.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			TagHelperContent content = await output.GetChildContentAsync();



			output.Content.AppendHtml(content);

			await Task.CompletedTask;
		}
	}
}
