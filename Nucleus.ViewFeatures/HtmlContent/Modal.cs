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

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders an icon button control.
	/// </summary>
	/// <remarks>
	///
	/// </remarks>
	internal static class Modal
	{
		internal static TagBuilder Build(ViewContext context, string title, Boolean canClose, TagHelperContent content, object htmlAttributes)
		{
			TagBuilder outputBuilder = new("div");
			TagBuilder dialogBuilder = new("div");
			TagBuilder contentBuilder = new("div");

			TagBuilder modalHeaderBuilder = new("div");
			TagBuilder modalTitleBuilder = new("h5");

			TagBuilder modalBodyBuilder = new("div");

			outputBuilder.AddCssClass("modal");
			outputBuilder.AddCssClass("fade");
			outputBuilder.MergeAttributes(htmlAttributes);

			dialogBuilder.AddCssClass("modal-dialog modal-auto-size modal-dialog-scrollable modal-dialog-centered");

			contentBuilder.AddCssClass("modal-content");

			modalHeaderBuilder.AddCssClass("modal-header");

			modalTitleBuilder.AddCssClass("modal-title");
			modalTitleBuilder.InnerHtml.Append(title);
			modalHeaderBuilder.InnerHtml.AppendHtml(modalTitleBuilder);

			if (canClose)
			{
				TagBuilder modalCloseButtonBuilder = new("button");
				modalCloseButtonBuilder.AddCssClass("btn-close");
				modalCloseButtonBuilder.Attributes.Add("type", "button");
				modalCloseButtonBuilder.Attributes.Add("data-bs-dismiss", "modal");
				modalCloseButtonBuilder.Attributes.Add("aria-label", "close");

				modalHeaderBuilder.InnerHtml.AppendHtml(modalCloseButtonBuilder);
			}

			contentBuilder.InnerHtml.AppendHtml(modalHeaderBuilder);

			modalBodyBuilder.AddCssClass("modal-body nucleus-admin-content");
			if (content != null)
			{
				modalBodyBuilder.InnerHtml.AppendHtml(content);
			}
			contentBuilder.InnerHtml.AppendHtml(modalBodyBuilder);

			dialogBuilder.InnerHtml.AppendHtml(contentBuilder);

			outputBuilder.InnerHtml.AppendHtml(dialogBuilder);

			return outputBuilder;
		}
	}
}
