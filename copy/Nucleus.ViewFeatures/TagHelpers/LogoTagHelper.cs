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
	///Returns an anchor (<![CDATA[a]]>) element which links to the site's home page, with an image inside which renders the site logo.
	/// </summary>	
	/// <remarks>
	/// If the site does not have a configured logo, nothing is rendered.
	/// </remarks>
	[HtmlTargetElement("Logo")]
	public class LogoTagHelper : TagHelper
	{		
		[ViewContext]
		public ViewContext ViewContext { get; set; }

		/// <summary>
		/// The inner text of the anchor element.
		/// </summary>
		public string Caption { get; set; }

		public LogoTagHelper()
		{
		
		}

		[EditorBrowsable(EditorBrowsableState.Never)]  // prevents inclusion in docfx-generated documentation
		async public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{			
			TagBuilder builder = Nucleus.ViewFeatures.HtmlContent.Logo.Build(this.ViewContext, this.Caption, null);

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