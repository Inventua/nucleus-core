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
	/// Returns an list (<![CDATA[ul]]>) element which contains the site's menu structure.
	/// </summary>	
	
	[HtmlTargetElement("Menu")]
	public class MenuTagHelper : TagHelper
	{		
		[ViewContext]
		public ViewContext ViewContext { get; set; }
				
		[HtmlAttributeName("class")]
		public string Class { get; set; }

		[HtmlAttributeName("maxLevels")]
		public int MaxLevels { get; set; } = 10;

		public MenuTagHelper()
		{
		
		}

		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			TagBuilder builder = Nucleus.ViewFeatures.HtmlContent.Menu.Build(this.ViewContext, this.MaxLevels, new { @Class = this.Class }) ;

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