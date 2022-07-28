using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Nucleus.ViewFeatures;
using System.ComponentModel.DataAnnotations;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Modules.Sitemap
{
	public enum RootPageTypes
	{
		[Display(Name = "Site Root")] SiteRoot = 0,
		[Display(Name = "Selected Page")] SelectedPage = 1,
		[Display(Name = "Home Page")] HomePage = 2,
		[Display(Name = "Current Page")] CurrentPage = 3,
		[Display(Name = "Parent Page")] ParentPage = 4,
		[Display(Name = "Top Ancestor")] TopAncestor = 5,
		[Display(Name = "Dual")] Dual = 6
	}

	/// <summary>
	/// Renders a site map.
	/// </summary>
	internal static class SitemapRenderer
	{

		internal static async Task<TagBuilder> Build(ViewContext context, RootPageTypes rootPageType, Guid selectedPageId, Boolean showDescription, int maxlevels, object htmlAttributes)
		{
			Page rootPage = null;
			TagBuilder outputBuilder;
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
			Context nucleusContext = context.HttpContext.RequestServices.GetService<Context>();
			IPageManager pageManager = context.HttpContext.RequestServices.GetService<IPageManager>();
			
			switch (rootPageType)
			{
				case RootPageTypes.SiteRoot:
					rootPage = null;
					break;
				case RootPageTypes.SelectedPage:
					rootPage = await pageManager.Get(selectedPageId);
					break;
				case RootPageTypes.HomePage:
					rootPage = await pageManager.Get(nucleusContext.Site, "/");
					break;
				case RootPageTypes.CurrentPage:
					rootPage = nucleusContext.Page;
					break;
				case RootPageTypes.ParentPage:
					rootPage = nucleusContext.Page.ParentId.HasValue ? await pageManager.Get(nucleusContext.Page.ParentId.Value) : null;
					break;
				case RootPageTypes.TopAncestor:
					rootPage = await GetTopAncestor(nucleusContext.Page, pageManager);
					break;
				case RootPageTypes.Dual:
					if (nucleusContext.Page.ParentId.HasValue)
					{
						rootPage = nucleusContext.Page;						
					}
					else
					{
						rootPage = await GetTopAncestor(nucleusContext.Page, pageManager);
					}
					break;
			}

			PageMenu pageMenu = await pageManager.GetMenu(nucleusContext.Site, rootPage, context.HttpContext.User, true);

			outputBuilder = AddPageChildren(nucleusContext, urlHelper, pageMenu.Children, showDescription, 0, maxlevels);

			if (outputBuilder != null)
			{
				outputBuilder.AddCssClass("Sitemap");
			}

			return outputBuilder;
		}

		private static async Task<Page> GetTopAncestor(Page page, IPageManager pageManager)
		{
			if (!page.ParentId.HasValue)
			{
				return page;
			}
			else
			{
				return await GetTopAncestor(await pageManager.Get(page.ParentId.Value), pageManager);
			}
		}

		private static TagBuilder AddPageChildren(Context nucleusContext, IUrlHelper urlHelper, IEnumerable<PageMenu> items, Boolean showDescription, int level, int maxlevels)
		{
			if (!items.Any())
			{
				return null;
			}	

			TagBuilder outputBuilder = new("ol");

			foreach (PageMenu menuItem in items)
			{
				if (menuItem.Page.ShowInMenu)
				{
					PageRoute route = menuItem.Page.DefaultPageRoute();
					string caption = !String.IsNullOrWhiteSpace(menuItem.Page.Title) ? menuItem.Page.Title : menuItem.Page.Name;

					TagBuilder listItemBuilder = new("li");

					if (!menuItem.Page.DisableInMenu && route != null)
					{
						TagBuilder linkBuilder = new("a");
						linkBuilder.InnerHtml.SetContent(caption);
						linkBuilder.Attributes.Add("href", urlHelper.GetAbsoluteUri(menuItem.Page.DefaultPageRoute().Path).AbsoluteUri);
						listItemBuilder.InnerHtml.AppendHtml(linkBuilder);
					}
					else
					{
						TagBuilder headingBuilder = new("h3");
						headingBuilder.InnerHtml.SetContent(caption);
						listItemBuilder.InnerHtml.AppendHtml(headingBuilder);						
					}

					if (showDescription && !String.IsNullOrEmpty(menuItem.Page.Description))
					{
						TagBuilder descriptionBuilder = new("div");
						descriptionBuilder.InnerHtml.SetContent(ContentExtensions.ToHtml(menuItem.Page.Description, "text/plain"));
						listItemBuilder.InnerHtml.AppendHtml(descriptionBuilder);
					}

					if (menuItem.Page.Id == nucleusContext.Page.Id)
					{
						listItemBuilder.AddCssClass("current-page");
					}

					if (maxlevels == 0 || level < maxlevels - 1)
					{
						listItemBuilder.InnerHtml.AppendHtml(AddPageChildren(nucleusContext, urlHelper, menuItem.Children, showDescription, level + 1, maxlevels));
					}

					outputBuilder.InnerHtml.AppendHtml(listItemBuilder);
				}
			}

			return outputBuilder;
		}
	}
}
