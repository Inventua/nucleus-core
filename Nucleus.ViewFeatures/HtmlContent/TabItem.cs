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
	/// Renders an icon button control.
	/// </summary>
	/// <remarks>
	///
	/// </remarks>
	/// <internal />
	/// <hidden />
	internal static class TabItem
	{
		internal static TagBuilder Build(ViewContext context, string target, string caption, Boolean active, Boolean enabled, Boolean alert, object htmlAttributes)
		{
			TagBuilder outputBuilder = new("li");
			TagBuilder buttonBuilder = new("button");

			outputBuilder.AddCssClass("nav-item");
			outputBuilder.Attributes.Add("role", "presentation");
			outputBuilder.MergeAttributes(htmlAttributes);

			buttonBuilder.AddCssClass("nav-link");
      if (!enabled)
      {
        buttonBuilder.AddCssClass("disabled");
      }
			buttonBuilder.InnerHtml.Append(caption);

      if (enabled && alert)
      {
        TagBuilder alertIcon = new("span");
        alertIcon.AddCssClass("nucleus-material-icon");
        alertIcon.InnerHtml.AppendHtml("&#xe000;");
        buttonBuilder.InnerHtml.AppendHtml(alertIcon);
      }

			buttonBuilder.Attributes.Add("data-bs-toggle", "tab");
			buttonBuilder.Attributes.Add("data-bs-target", target);
			buttonBuilder.Attributes.Add("type", "button");
			buttonBuilder.Attributes.Add("role", "tab");
			buttonBuilder.Attributes.Add("aria-controls", target.Replace("#", String.Empty));

			if (active)
			{
				buttonBuilder.AddCssClass("active");
			}

			outputBuilder.InnerHtml.AppendHtml(buttonBuilder);

			return outputBuilder;			
		}		
	}
}
