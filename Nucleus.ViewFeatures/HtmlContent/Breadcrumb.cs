using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Threading.Tasks;

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders a breadcrumb control.
	/// </summary>
	/// <remarks>
	/// This function is used by the <see cref="HtmlHelpers.BreadcrumbHtmlHelper"/> and <see cref="TagHelpers.BreadcrumbTagHelper"/>.
	/// </remarks>
	/// <internal />
	/// <hidden />
	internal static class Breadcrumb
	{
		internal static async Task<TagBuilder> Build(ViewContext context, Boolean hideTopLevel, object htmlAttributes)
		{
			TagBuilder outputBuilder = new("nav");
			TagBuilder listBuilder = new("ol");

			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
			Context nucleusContext = context.HttpContext.RequestServices.GetService<Context>();
			IPageManager pageManager = context.HttpContext.RequestServices.GetService<IPageManager>();

			List<Page> breadcrumbs = new();
			outputBuilder.Attributes.Add("area-label", "breadcrumb");
			listBuilder.AddCssClass("breadcrumb");

			Page breadcrumbPage = nucleusContext.Page;
			do
			{
				if (breadcrumbPage != null)
				{
					breadcrumbs.Add(breadcrumbPage);
				}

				if (breadcrumbPage.ParentId.HasValue)
				{
					breadcrumbPage = await pageManager.Get(breadcrumbPage.ParentId.Value);					
				}
				else
				{
					breadcrumbPage = null;
				}
			} while (breadcrumbPage != null);

			if (breadcrumbs.Count == 0 || (breadcrumbs.Count == 1 && hideTopLevel))
			{
				return null;
			}			
			else
			{
				breadcrumbs.Reverse();

				foreach (Page page in breadcrumbs)
				{
					if (page.ShowInMenu)
					{
						TagBuilder listItemBuilder = new("li");
						string caption = String.IsNullOrEmpty(page.Title) ? page.Name : page.Title;

						listItemBuilder.AddCssClass("breadcrumb-item");

						if (page.Id == nucleusContext.Page.Id)
						{
							listItemBuilder.AddCssClass("active");
						}

						if (page.DisableInMenu)
						{
							TagBuilder breadcrumbSpanBuilder = new("span");
							breadcrumbSpanBuilder.InnerHtml.SetContent(caption);
							
							listItemBuilder.InnerHtml.AppendHtml(breadcrumbSpanBuilder);
						}
						else
						{
							TagBuilder breadcrumbLinkBuilder = new("a");
							breadcrumbLinkBuilder.InnerHtml.SetContent(caption);
							breadcrumbLinkBuilder.Attributes.Add("href", urlHelper.GetAbsoluteUri(page.DefaultPageRoute().Path).AbsoluteUri);

							listItemBuilder.InnerHtml.AppendHtml(breadcrumbLinkBuilder);
						}

						listBuilder.InnerHtml.AppendHtml(listItemBuilder);
					}
				}
				
				outputBuilder.InnerHtml.AppendHtml(listBuilder);
				outputBuilder.MergeAttributes(htmlAttributes);

				return outputBuilder;
			}
		}
	}
}
