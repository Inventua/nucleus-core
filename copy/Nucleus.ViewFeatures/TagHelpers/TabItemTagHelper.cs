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
	/// Renders an tab item
	/// </summary>
	[HtmlTargetElement("TabItem", ParentTag = "Tab")]
	
	public class TabItemTagHelper : TagHelper
	{		
		[ViewContext]
		public ViewContext ViewContext { get; set; }

		public string Target { get; set; }
		public string Caption { get; set; }
		public Boolean Active { get; set; }

		public TabItemTagHelper() { }

		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			TagBuilder builder = Nucleus.ViewFeatures.HtmlContent.TabItem.Build(this.ViewContext, this.Target, this.Caption, this.Active, null);

			output.TagName = builder.TagName;
			output.MergeAttributes(builder);
			output.Content.AppendHtml(builder.InnerHtml);

			await Task.CompletedTask;
		}
	}
}