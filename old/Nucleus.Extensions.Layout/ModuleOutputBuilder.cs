//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Html;
//using Microsoft.AspNetCore.Razor.TagHelpers;
//using Microsoft.AspNetCore.Mvc.Rendering;

//namespace Nucleus.Extensions.Layout
//{
//	public static class ModuleOutputBuilder
//	{
//		internal static TagBuilder Build(IHtmlContent content, Boolean RenderInline)
//		{
//			TagBuilder builder;
//			string tagName = RenderInline ? "span" : "div";

//			builder = new TagBuilder(tagName);

//			builder.AddCssClass("Module");
//			//builder.MergeAttributes(htmlAttributes);
//			builder.InnerHtml.AppendHtml(content);

//			return builder;
//		}
//	}
//}
