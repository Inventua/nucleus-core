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
	/// Returns an list (<![CDATA[ul]]>) element which contains the site's menu structure.
	/// </summary>	
	
	[HtmlTargetElement("Menu")]
	public class MenuTagHelper : TagHelper
	{
		/// <summary>
		/// Provides access to view context.
		/// </summary>
		[ViewContext]
		[HtmlAttributeNotBound]
		public ViewContext ViewContext { get; set; }
				
		/// <summary>
		/// Number of menu levels to display, or -1 for all levels.
		/// </summary>
		[HtmlAttributeName("maxLevels")]
		public int MaxLevels { get; set; } = 10;

		/// <summary>
		/// Menu style
		/// </summary>
		[HtmlAttributeName("menuStyle")]
		public HtmlContent.Menu.MenuStyles MenuStyle { get; set; } = HtmlContent.Menu.MenuStyles.DropDown;

		/// <summary>
		/// Generate the output.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			TagBuilder builder = await Nucleus.ViewFeatures.HtmlContent.Menu.Build(this.ViewContext, this.MenuStyle, this.MaxLevels, null) ;

			if (builder == null)
			{
				output.SuppressOutput();
			}
			else
			{
				output.TagMode = TagMode.StartTagAndEndTag;
				output.TagName = builder.TagName;
				output.MergeAttributes(builder);
				output.Content.AppendHtml(builder.InnerHtml);
			}
			await Task.CompletedTask;
		}
	}
}