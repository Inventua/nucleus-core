﻿using System;
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
	[HtmlTargetElement("Tab")]
	[RestrictChildren("TabItem")]
	public class TabTagHelper : TagHelper
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
			TagBuilder builder = Nucleus.ViewFeatures.HtmlContent.Tab.Build(this.ViewContext, await output.GetChildContentAsync(), null);

			output.TagName = builder.TagName;
			output.TagMode = TagMode.StartTagAndEndTag;

			output.MergeAttributes(builder);
			output.Content.AppendHtml(builder.InnerHtml);
		}
	}
}