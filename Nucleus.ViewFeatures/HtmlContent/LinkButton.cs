using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;


//<a href="@Url.AreaAction("Index", "FileSystem", "Admin", new { path = Model.Current.ParentPath })" class="NavigateLink iconbutton" data-target=".nucleus-adminpage">
//	<label>
//		<span class='nucleus-material-icon'>&#xe5c4;</span>
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
	internal static class LinkButton
	{
		internal static TagBuilder Build(ViewContext context, string glyph, string caption, string href, Boolean buttonStyle, object htmlAttributes)
		{
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
						
			TagBuilder outputBuilder = new("a");
			TagBuilder labelBuilder = new("label");
			TagBuilder spanBuilder = new("span");
			Boolean blnSuppressAutoClass = false;

			object className = TagBuilderExtensions.GetAttribute(htmlAttributes, "class");
			if (className != null && className is string)
			{
				if ((className as string).Contains("btn"))
				{
					blnSuppressAutoClass = true;
				}
			}

			if (!blnSuppressAutoClass && buttonStyle)
			{
				outputBuilder.AddCssClass("btn btn-secondary");
			}

			outputBuilder.Attributes.Add("href", urlHelper.Content(href));
			outputBuilder.MergeAttributes(htmlAttributes);

			if (!String.IsNullOrEmpty(glyph))
			{
				spanBuilder.InnerHtml.SetHtmlContent(glyph);
				spanBuilder.AddCssClass("nucleus-material-icon");

				labelBuilder.InnerHtml.AppendHtml(spanBuilder);
			}

			labelBuilder.InnerHtml.Append(caption);

			outputBuilder.InnerHtml.AppendHtml(labelBuilder);

			return outputBuilder;			
		}		
	}
}
