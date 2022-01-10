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
using Nucleus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders an Breadcrumb control.
	/// </summary>
	/// <remarks>
	///   
	/// This function is used by the <see cref="HtmlHelpers.BreadcrumbHtmlHelper"/> and <see cref="TagHelpers.BreadcrumbTagHelper"/>.  The output is structured per
	/// https://developers.google.com/search/docs/data-types/breadcrumb#html
	/// </remarks>
	internal static class Breadcrumb
	{

		internal static TagBuilder Build(ViewContext context, object htmlAttributes)
		{
			TagBuilder outputBuilder = new TagBuilder("nav");
			TagBuilder listBuilder = new TagBuilder("ol");

			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
			Context nucleusContext = context.HttpContext.RequestServices.GetService<Context>();
			PageManager pageManager = context.HttpContext.RequestServices.GetService<PageManager>();

			List<Page> breadcrumbs = new();
			Page breadcrumbPage = null;

			outputBuilder.Attributes.Add("area-label", "breadcrumb");
			listBuilder.AddCssClass("breadcrumb");

			breadcrumbPage = nucleusContext.Page;
			do
			{
				if (breadcrumbPage != null)
				{
					breadcrumbs.Add(breadcrumbPage);
				}

				if (breadcrumbPage.ParentId.HasValue)
				{
					breadcrumbPage = pageManager.Get(breadcrumbPage.ParentId.Value);					
				}
				else
				{
					breadcrumbPage = null;
				}
			} while (breadcrumbPage != null);

			if (breadcrumbs.Count == 0)
			{
				return null;
			}
			else
			{
				breadcrumbs.Reverse();

				foreach (Page page in breadcrumbs)
				{
					TagBuilder listItemBuilder = new TagBuilder("li");
					string caption = String.IsNullOrEmpty(page.Title) ? page.Name : page.Title;

					listItemBuilder.AddCssClass("breadcrumb-item");

					if (page.Id != nucleusContext.Page.Id)
					{
						TagBuilder breadcrumbLinkBuilder = new TagBuilder("a");
						breadcrumbLinkBuilder.InnerHtml.SetContent(caption);
						breadcrumbLinkBuilder.Attributes.Add("href", urlHelper.GetAbsoluteUri(page.DefaultPageRoute().Path).AbsoluteUri);

						listItemBuilder.InnerHtml.AppendHtml(breadcrumbLinkBuilder);
					}
					else
					{
						listItemBuilder.AddCssClass("active");
						listItemBuilder.InnerHtml.SetContent(caption);
					}

					listBuilder.InnerHtml.AppendHtml(listItemBuilder);
				}

				//outputBuilder.AddCssClass("Breadcrumbs");
				outputBuilder.InnerHtml.AppendHtml(listBuilder);
				outputBuilder.MergeAttributes(htmlAttributes);

				return outputBuilder;
			}
		}
	}
}
