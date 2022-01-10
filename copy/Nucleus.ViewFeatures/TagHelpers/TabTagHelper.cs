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
	/// Renders an account control.
	/// </summary>
	[HtmlTargetElement("Tab")]
	[RestrictChildren("TabItem")]
	public class TabTagHelper : TagHelper
	{		
		[ViewContext]
		public ViewContext ViewContext { get; set; }

		public TabTagHelper() { }

		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			TagBuilder builder = Nucleus.ViewFeatures.HtmlContent.Tab.Build(this.ViewContext, await output.GetChildContentAsync(), null);

			output.TagName = builder.TagName;
			output.MergeAttributes(builder);
			output.Content.AppendHtml(builder.InnerHtml);
		}
	}
}