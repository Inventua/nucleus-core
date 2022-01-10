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
	[HtmlTargetElement("TabPanel", ParentTag = "TabContent")]
	
	public class TabPanelTagHelper : TagHelper
	{
		/// <summary>
		/// Provides access to view context.
		/// </summary>
		[ViewContext]
		[HtmlAttributeNotBound]
		public ViewContext ViewContext { get; set; }

		/// <summary>
		/// Control id
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Specifies whether the panel is selected.
		/// </summary>
		public Boolean Active { get; set; }

		/// <summary>
		/// Generate the output.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			TagBuilder builder = Nucleus.ViewFeatures.HtmlContent.TabPanel.Build(this.ViewContext, this.Id, this.Active, null);

			output.TagName = builder.TagName;
			output.MergeAttributes(builder);

			output.Content.AppendHtml(await output.GetChildContentAsync());
		}
	}
}