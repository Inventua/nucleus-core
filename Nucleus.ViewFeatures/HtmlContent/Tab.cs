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
using Nucleus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders a tab
	/// </summary>
	/// <remarks>
	///
	/// </remarks>
	/// <internal />
	/// <hidden />
	internal static class Tab
	{
		internal static TagBuilder Build(ViewContext context, TagHelperContent content, object htmlAttributes)
		{
			TagBuilder outputBuilder = new("nav");
			TagBuilder listBuilder = new("ul");

			outputBuilder.MergeAttributes(htmlAttributes);

			listBuilder.AddCssClass("nav nav-tabs");
			listBuilder.Attributes.Add("role", "tablist");

			listBuilder.InnerHtml.AppendHtml(content);

			outputBuilder.InnerHtml.AppendHtml(listBuilder);

			return outputBuilder;			
		}		
	}
}
