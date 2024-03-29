﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Models;
using Microsoft.AspNetCore.Http;
using Nucleus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;


//<input type="submit" formaction="@Url.AreaAction("ShowDeleteDialog", "FileSystem", "Admin")" value="Delete" title="Delete" class="btn btn-danger" data-target=".FileSystemEditor" />


namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders an icon button control.
	/// </summary>
	/// <remarks>
	///
	/// </remarks>
	/// <internal />
	/// <hidden />
	internal static class SubmitButton
	{
		internal static TagBuilder Build(ViewContext context, string glyph, string caption, string formaction, object htmlAttributes)
		{
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
						
			TagBuilder outputBuilder = new("button");
			Boolean blnSuppressAutoClass = false;

			outputBuilder.Attributes.Add("type", "submit");

			object className = TagBuilderExtensions.GetAttribute(htmlAttributes, "class");
			if (className != null && className is string)
			{
				if ((className as string).Contains("btn"))
				{
					blnSuppressAutoClass = true;
				}
			}

			if (!blnSuppressAutoClass)
			{
				outputBuilder.AddCssClass("btn btn-primary");
			}

			outputBuilder.Attributes.Add("formaction", urlHelper.Content(formaction));
			outputBuilder.MergeAttributes(htmlAttributes);

			if (!string.IsNullOrEmpty(glyph))
			{
				TagBuilder spanBuilder = new("span");
				spanBuilder.InnerHtml.SetHtmlContent(glyph);
				spanBuilder.AddCssClass("nucleus-material-icon");

				if (!String.IsNullOrEmpty(caption))
				{ 
					spanBuilder.AddCssClass("me-1");
					TagBuilder labelBuilder = new("div");

					labelBuilder.InnerHtml.AppendHtml(spanBuilder);

					labelBuilder.AddCssClass("d-flex");		
					labelBuilder.InnerHtml.Append(caption);

					outputBuilder.InnerHtml.AppendHtml(labelBuilder);
				}
				else
				{
					outputBuilder.InnerHtml.AppendHtml(spanBuilder);
				}
			}
			else
			{
				outputBuilder.InnerHtml.Append(caption);
			}

			return outputBuilder;			
		}		
	}
}
