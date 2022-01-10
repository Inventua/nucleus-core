//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Razor.TagHelpers;
//using Microsoft.AspNetCore.Html;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Nucleus.Abstractions.Models;
//using Microsoft.AspNetCore.Http;
//using Nucleus.Core;
//using Nucleus.Core.Authorization;
//using Nucleus.Abstractions;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Routing;

//namespace Nucleus.ViewFeatures.HtmlContent
//{
//	public static class SettingsControl
//	{
//		internal static TagBuilder Build(ViewContext context, string caption, string helpText, Nucleus.ViewFeatures.TagHelpers.SettingsControlTagHelper.RenderModes renderMode, object htmlAttributes)
//		{
//			TagBuilder outputBuilder = new TagBuilder("DIV");
//			TagBuilder builder = new TagBuilder("div");
//			TagBuilder labelBuilder = new TagBuilder("label");
//			TagBuilder labelSpanBuilder = new TagBuilder("span");

//			outputBuilder.AddCssClass("settings-control");
//			outputBuilder.Attributes.Add("title", helpText);
//			labelSpanBuilder.InnerHtml.Append(caption);

//			switch (renderMode)
//			{
//				case TagHelpers.SettingsControlTagHelper.RenderModes.LabelFirst:
//					labelBuilder.InnerHtml.AppendHtml(labelSpanBuilder);
//					labelBuilder.InnerHtml.AppendHtml(content);
//					break;

//				case TagHelpers.SettingsControlTagHelper.RenderModes.LabelLast:
//					labelBuilder.InnerHtml.AppendHtml(content);
//					labelBuilder.InnerHtml.AppendHtml(labelSpanBuilder);
//					break;
//			}

//			builder.InnerHtml.AppendHtml(labelBuilder);
//			outputBuilder.InnerHtml.AppendHtml(builder.InnerHtml);

//			outputBuilder.MergeAttributes(htmlAttributes);

//			return outputBuilder;
//		}		
//	}
//}
