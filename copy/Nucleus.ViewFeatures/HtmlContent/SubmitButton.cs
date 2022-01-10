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


//<input type="submit" formaction="@Url.AreaAction("ShowDeleteDialog", "FileSystem", "Admin")" value="Delete" title="Delete" class="DeleteButton" data-target=".FileSystemEditor" />


namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders an icon button control.
	/// </summary>
	/// <remarks>
	///
	/// </remarks>
	public static class SubmitButton
	{
		internal static TagBuilder Build(ViewContext context, string glyph, string caption, string formaction, object htmlAttributes)
		{
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
						
			TagBuilder outputBuilder = new TagBuilder("button");
			TagBuilder labelBuilder = new TagBuilder("div");
			TagBuilder spanBuilder = new TagBuilder("span");

			outputBuilder.Attributes.Add("type", "submit");
			outputBuilder.AddCssClass("NavigateLink");
			outputBuilder.Attributes.Add("formaction", urlHelper.Content(formaction));
			outputBuilder.MergeAttributes(htmlAttributes);

			spanBuilder.InnerHtml.SetHtmlContent(glyph);
			spanBuilder.AddCssClass("MaterialIcon");

			labelBuilder.InnerHtml.AppendHtml(spanBuilder);
			labelBuilder.InnerHtml.Append(caption);

			outputBuilder.InnerHtml.AppendHtml(labelBuilder);

			return outputBuilder;			
		}		
	}
}
