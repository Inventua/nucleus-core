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
using Nucleus.ViewFeatures;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Modules.Sitemap
{
	public enum RootPageTypes
	{
		[Display(Description = "Selected Page")] SelectedPage = 0,
		[Display(Description = "Home Page")] HomePage = 1,
		[Display(Description = "Current Page")] CurrentPage = 2,
		[Display(Description = "Parent Page")] ParentPage = 3,
		[Display(Description = "Top Ancestor")] TopAncestor = 4,
		[Display(Description = "Dual")] Dual = 5
	}

	/// <summary>
	/// Renders an site map.
	/// </summary>
	/// <remarks>
	///   
	/// This function is used by the <see cref="HtmlHelpers.SitemapHtmlHelper"/> and <see cref="TagHelpers.BreadcrumbTagHelper"/>.  The output is structured per
	/// https://developers.google.com/search/docs/data-types/breadcrumb#html
	/// </remarks>
	internal static class SitemapRenderer
	{

		internal static TagBuilder Build(ViewContext context, RootPageTypes rootPageType, Guid selectedPageId, Boolean showDescription, int maxlevels, object htmlAttributes)
		{
			Page rootPage = null;
			TagBuilder outputBuilder;
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
			Context nucleusContext = context.HttpContext.RequestServices.GetService<Context>();
			PageManager pageManager = context.HttpContext.RequestServices.GetService<PageManager>();
			
			switch (rootPageType)
			{
				case RootPageTypes.SelectedPage:
					rootPage = pageManager.Get(selectedPageId);
					break;
				case RootPageTypes.HomePage:
					rootPage = pageManager.Get(nucleusContext.Site, "/");
					break;
				case RootPageTypes.CurrentPage:
					rootPage = nucleusContext.Page;
					break;
				case RootPageTypes.ParentPage:
					rootPage = nucleusContext.Page.ParentId.HasValue ? pageManager.Get(nucleusContext.Page.ParentId.Value) : null;
					break;
				case RootPageTypes.TopAncestor:
					rootPage = GetTopAncestor(nucleusContext.Page, pageManager);
					break;
				case RootPageTypes.Dual:
					if (nucleusContext.Page.ParentId.HasValue)
					{
						rootPage = nucleusContext.Page;						
					}
					else
					{
						rootPage = GetTopAncestor(nucleusContext.Page, pageManager);
					}
					break;
			}

			PageMenu pageMenu = pageManager.GetMenu(nucleusContext.Site, rootPage, context.HttpContext.User, true);

			outputBuilder = AddPageChildren(urlHelper, pageMenu.Children, showDescription, 0, maxlevels);
			outputBuilder.AddCssClass("Sitemap");
			return outputBuilder;
		}

		private static Page GetTopAncestor(Page page, PageManager pageManager)
		{
			if (!page.ParentId.HasValue)
			{
				return page;
			}
			else
			{
				return GetTopAncestor(pageManager.Get(page.ParentId.Value), pageManager);
			}
		}

		private static TagBuilder AddPageChildren(IUrlHelper urlHelper, IEnumerable<PageMenu> items, Boolean showDescription, int level, int maxlevels)
		{
			TagBuilder outputBuilder = new TagBuilder("ol");

			foreach (PageMenu menuItem in items)
			{
				PageRoute route = menuItem.Page.DefaultPageRoute();
				string caption = !String.IsNullOrWhiteSpace(menuItem.Page.Title) ? menuItem.Page.Title : menuItem.Page.Name;

				TagBuilder listItemBuilder = new("li");
					
				if (route != null)
				{
					TagBuilder linkBuilder = new TagBuilder("a");
					linkBuilder.InnerHtml.SetContent(caption);
					linkBuilder.Attributes.Add("href", urlHelper.GetAbsoluteUri(menuItem.Page.DefaultPageRoute().Path).AbsoluteUri);
					listItemBuilder.InnerHtml.AppendHtml(linkBuilder);				
				}
				else
				{
					listItemBuilder.InnerHtml.SetContent(caption);
				}

				if (showDescription && !String.IsNullOrEmpty(menuItem.Page.Description))
				{
					TagBuilder descriptionBuilder = new("div");
					descriptionBuilder.InnerHtml.SetContent(menuItem.Page.Description);
					listItemBuilder.InnerHtml.AppendHtml(descriptionBuilder);
				}

				if (maxlevels==0 || level < maxlevels-1)
				{
					listItemBuilder.InnerHtml.AppendHtml(AddPageChildren(urlHelper, menuItem.Children, showDescription, level + 1, maxlevels));
				}

				outputBuilder.InnerHtml.AppendHtml(listItemBuilder);
				
			}

			return outputBuilder;
		}
	}
}
