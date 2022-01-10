using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Http;
using Nucleus.Core;
using Nucleus.Core.Authorization;
using Nucleus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;


//<a href="@Url.AreaAction("Index", "FileSystem", "Admin", new { path = Model.Current.ParentPath })" class="NavigateLink iconbutton" data-target=".AdminPage">
//	<label>
//		<span class='MaterialIcon'>&#xe5c4;</span>
//		Back
//	</label>
//</a>

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders an icon button control.
	/// </summary>
	/// <remarks>
	///
	/// </remarks>
	public static class LinkButton
	{
		internal static TagBuilder Build(ViewContext context, string glyph, string caption, string href, Boolean buttonStyle, object htmlAttributes)
		{
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
						
			TagBuilder outputBuilder = new TagBuilder("a");
			TagBuilder labelBuilder = new TagBuilder("label");
			TagBuilder spanBuilder = new TagBuilder("span");

			if (buttonStyle)
			{
				outputBuilder.AddCssClass("NavigateLink button");
			}

			outputBuilder.Attributes.Add("href", urlHelper.Content(href));
			outputBuilder.MergeAttributes(htmlAttributes);

			if (!String.IsNullOrEmpty(glyph))
			{
				spanBuilder.InnerHtml.SetHtmlContent(glyph);
				spanBuilder.AddCssClass("MaterialIcon");

				labelBuilder.InnerHtml.AppendHtml(spanBuilder);
			}

			labelBuilder.InnerHtml.Append(caption);

			outputBuilder.InnerHtml.AppendHtml(labelBuilder);

			return outputBuilder;			
		}		
	}
}
