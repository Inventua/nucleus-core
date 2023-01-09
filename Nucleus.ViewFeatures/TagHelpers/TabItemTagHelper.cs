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
	/// Renders an tab item
	/// </summary>
	[HtmlTargetElement("TabItem", ParentTag = "Tab")]
	
	public class TabItemTagHelper : TagHelper
	{
		/// <summary>
		/// Provides access to view context.
		/// </summary>
		[ViewContext]
		[HtmlAttributeNotBound]
		public ViewContext ViewContext { get; set; }

		/// <summary>
		/// Id (including #) of the <seealso cref="TabPanelTagHelper"/> which this tab item represents.
		/// </summary>
		public string Target { get; set; }

		/// <summary>
		/// Label text
		/// </summary>
		public string Caption { get; set; }

		/// <summary>
		/// Specifies whether the tab is selected.
		/// </summary>
		public Boolean Active { get; set; }

    /// <summary>
		/// Specifies whether the tab displays an alert symbol.
		/// </summary>
		public Boolean Alert { get; set; }

    /// <summary>
		/// Specifies whether the tab is enabled.
		/// </summary>
		public Boolean Enabled { get; set; } = true;

    /// <summary>
    /// Generate the output.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			TagBuilder builder = Nucleus.ViewFeatures.HtmlContent.TabItem.Build(this.ViewContext, this.Target, this.Caption, this.Active, this.Enabled, this.Alert, null);

			output.TagName = builder.TagName;
			output.TagMode = TagMode.StartTagAndEndTag;

			output.MergeAttributes(builder);
			output.Content.AppendHtml(builder.InnerHtml);

			await Task.CompletedTask;
		}
	}
}