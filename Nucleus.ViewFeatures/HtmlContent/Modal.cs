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
	/// <internal />
	/// <hidden />
	internal static class Modal
	{
		private static string[] modalSizes = { "modal-lg", "modal-xl", "modal-sm", "modal-default" };

		internal static TagBuilder Build(ViewContext context, string title, Boolean canClose, TagHelperContent content, string innerClass, Boolean renderFooter, Boolean useAdminStyles, object htmlAttributes)
		{
			Boolean isAutoSize = false;

			TagBuilder outputBuilder = new("div");
			TagBuilder dialogBuilder = new("div");
			TagBuilder contentBuilder = new("div");

			TagBuilder modalHeaderBuilder = new("div");
			TagBuilder modalTitleBuilder = new("h5");

			TagBuilder modalBodyBuilder = new("div");

      outputBuilder.AddCssClass("modal");
			outputBuilder.AddCssClass("fade");
			outputBuilder.MergeAttributes(htmlAttributes);

			dialogBuilder.AddCssClass(innerClass);
			dialogBuilder.AddCssClass("modal-dialog modal-dialog-scrollable modal-dialog-centered");
			

			if (innerClass == null || !innerClass.Split(' ').Any(value => modalSizes.Contains(value)))				
			{
				dialogBuilder.AddCssClass("modal-auto-size");
				isAutoSize = true;
			}

			contentBuilder.AddCssClass("modal-content");

			modalHeaderBuilder.AddCssClass("modal-header");

			modalTitleBuilder.AddCssClass("modal-title");
      modalTitleBuilder.Attributes.Add("data-original-text", title);
      modalTitleBuilder.InnerHtml.Append(title);
			modalHeaderBuilder.InnerHtml.AppendHtml(modalTitleBuilder);

      TagBuilder modalHelpButtonBuilder = new("a");
      modalHelpButtonBuilder.AddCssClass("btn btn-help nucleus-material-icon collapse");
      modalHelpButtonBuilder.Attributes.Add("target", "_blank");
      modalHelpButtonBuilder.Attributes.Add("aria-label", "Help");
      modalHelpButtonBuilder.InnerHtml.AppendHtml("&#xe887;");

      modalHeaderBuilder.InnerHtml.AppendHtml(modalHelpButtonBuilder);

      if (isAutoSize)
			{
				TagBuilder modalMinimizeButtonBuilder = new("button");
				modalMinimizeButtonBuilder.AddCssClass("btn btn-normalsize nucleus-material-icon");
				modalMinimizeButtonBuilder.Attributes.Add("type", "button");
				modalMinimizeButtonBuilder.Attributes.Add("aria-label", "Normal size");
				modalMinimizeButtonBuilder.InnerHtml.AppendHtml("&#xe5d1;");

				modalHeaderBuilder.InnerHtml.AppendHtml(modalMinimizeButtonBuilder);

				TagBuilder modalMaximizeButtonBuilder = new("button");
				modalMaximizeButtonBuilder.AddCssClass("btn btn-maximize nucleus-material-icon");
				modalMaximizeButtonBuilder.Attributes.Add("type", "button");
				modalMaximizeButtonBuilder.Attributes.Add("aria-label", "Maximize");
				modalMaximizeButtonBuilder.InnerHtml.AppendHtml("&#xe5d0;");

				modalHeaderBuilder.InnerHtml.AppendHtml(modalMaximizeButtonBuilder);
			}

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

			modalBodyBuilder.AddCssClass("modal-body");

      if (useAdminStyles)
      {
        modalBodyBuilder.AddCssClass("nucleus-admin-content");
      }

      if (content != null && !content.IsEmptyOrWhiteSpace)
			{
				modalBodyBuilder.InnerHtml.AppendHtml(content);
			}
			contentBuilder.InnerHtml.AppendHtml(modalBodyBuilder);

      if (renderFooter)
      {
        TagBuilder modalFooterBuilder = new("div");
        modalFooterBuilder.AddCssClass("modal-footer");
        contentBuilder.InnerHtml.AppendHtml(modalFooterBuilder);
      }

      dialogBuilder.InnerHtml.AppendHtml(contentBuilder);

			outputBuilder.InnerHtml.AppendHtml(dialogBuilder);

			return outputBuilder;
		}
	}
}
