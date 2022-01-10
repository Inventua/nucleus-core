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
using Nucleus.Core;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Mvc.TagHelpers;

namespace Nucleus.ViewFeatures.TagHelpers
{
	/// <summary>
	/// Renders an Breadcrumb control.
	/// </summary>
	[HtmlTargetElement("Breadcrumb")]
	public class BreadcrumbTagHelper : TagHelper
	{
		[ViewContext]
		public ViewContext ViewContext { get; set; }

		public BreadcrumbTagHelper() { }

		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			TagBuilder builder = Nucleus.ViewFeatures.HtmlContent.Breadcrumb.Build(this.ViewContext, null);

			output.TagName = builder.TagName;
			output.MergeAttributes(builder);
			output.Content.AppendHtml(builder.InnerHtml);

			await Task.CompletedTask;
		}
	}
}