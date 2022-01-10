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
	/// Renders a content wrapper for tab contents
	/// </summary>
	[HtmlTargetElement("TabContent")]
	[RestrictChildren("TabPanel")]
	public class TabContentTagHelper : TagHelper
	{		
		[ViewContext]
		public ViewContext ViewContext { get; set; }
			
		public TabContentTagHelper() { }

		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			TagBuilder builder = Nucleus.ViewFeatures.HtmlContent.TabContent.Build(this.ViewContext, null);

			output.TagName = builder.TagName;
			output.MergeAttributes(builder);

			builder.InnerHtml.AppendHtml(await output.GetChildContentAsync());

			output.Content.AppendHtml(builder.InnerHtml);
		}
	}
}