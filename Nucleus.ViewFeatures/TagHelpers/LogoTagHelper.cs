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
	///Returns an anchor (<![CDATA[a]]>) element which links to the site's home page, with an image inside which renders the site logo.
	/// </summary>	
	/// <remarks>
	/// If the site does not have a configured logo, nothing is rendered.
	/// </remarks>
	[HtmlTargetElement("Logo")]
	public class LogoTagHelper : TagHelper
	{
		/// <summary>
		/// Provides access to view context.
		/// </summary>
		[ViewContext]
		[HtmlAttributeNotBound]
		public ViewContext ViewContext { get; set; }

		/// <summary>
		/// The inner text of the anchor element.
		/// </summary>
		public string Caption { get; set; }

    /// <summary>
    /// Specifies whether to render the site title if no logo file could be found
    /// </summary>
    public Boolean FallbackToSiteTitle { get; set; } = true;

		/// <summary>
		/// Generate the output.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{			
			TagBuilder builder = await Nucleus.ViewFeatures.HtmlContent.Logo.Build(this.ViewContext, this.Caption, this.FallbackToSiteTitle, null);

			if (builder == null)
			{
				output.SuppressOutput();
			}
			else
			{
				output.TagName = builder.TagName;
				output.TagMode = TagMode.StartTagAndEndTag;
				output.MergeAttributes(builder);
				output.Content.AppendHtml(builder.InnerHtml);
			}

			await Task.CompletedTask;
		}
	}
}