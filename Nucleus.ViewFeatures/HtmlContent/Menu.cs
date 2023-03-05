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
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// 
	/// </summary>
	/// <internal />
	/// <hidden />
	public class Menu
	{
		/// <summary>
		/// Menu rendering styles used by the <see cref="TagHelpers.MenuTagHelper"/> and <see cref="HtmlHelpers.MenuHtmlHelper"/>.
		/// </summary>
		public enum MenuStyles
		{
			/// <summary>
			/// Drop-down/Flyout style menu
			/// </summary>
			DropDown = 0,
			/// <summary>
			/// Child items rendered horizontally as list blocks
			/// </summary>
			RibbonLandscape = 1,
			/// <summary>
			/// Child items rendered vertically as list blocks
			/// </summary>
			RibbonPortrait = 2,
      /// <summary>
      /// Horizontal display
      /// </summary>
      Horizontal = 3
		}

    /// <summary>
    /// Menu root page types used by the  <see cref="TagHelpers.MenuTagHelper"/> and <see cref="HtmlHelpers.MenuHtmlHelper"/>.
    /// </summary>
    public enum RootPageTypes
    {
      /// <summary>
      /// Start from the root of the site (pages with no parent set).
      /// </summary>
      [Display(Name = "Site Root")] SiteRoot = 0,
      /// <summary>
      /// Display pages starting from the selected page, specified by rootPageId. 
      /// </summary>
      [Display(Name = "Selected Page")] SelectedPage = 1,
      /// <summary>
      /// Display pages starting from the site's home page.  
      /// </summary>
      [Display(Name = "Home Page")] HomePage = 2,
      /// <summary>
      /// Display pages starting with the children of the current page.
      /// </summary>
      [Display(Name = "Current Page")] CurrentPage = 3,
      /// <summary>
      /// Display pages starting with the current page's parent.
      /// </summary>
      [Display(Name = "Parent Page")] ParentPage = 4,
      /// <summary>
      /// Display pages starting with the top ancestor of the current page (the ancestor with no parent page set). 
      /// </summary>
      [Display(Name = "Top Ancestor")] TopAncestor = 5,
      /// <summary>
      /// If the current page has a parent, use the same behavior as `Current Page`.  If the current page does not have a parent, use the same behaviour as `Top Ancestor`.
      /// </summary>
      [Display(Name = "Dual")] Dual = 6
    }


    internal static async Task<TagBuilder> Build(ViewContext context, MenuStyles menuStyle, RootPageTypes rootPageType, Guid rootPageId, Boolean hideEmptyMenu, int maxLevels, object htmlAttributes)
    {
      IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
      Context nucleusContext = context.HttpContext.RequestServices.GetService<Context>();
      IPageManager pageManager = context.HttpContext.RequestServices.GetService<IPageManager>();
      Page rootPage = null;

      switch (rootPageType)
      {
        case RootPageTypes.SiteRoot:
          rootPage = null;
          break;
        case RootPageTypes.SelectedPage:
          rootPage = await pageManager.Get(rootPageId);
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

      PageMenu topMenu = await pageManager.GetMenu(nucleusContext.Site, rootPage, context.HttpContext.User, false);

      if (hideEmptyMenu && !topMenu.HasChildren)
      {
        return null;
      }

      TagBuilder outputBuilder = new("nav");
      outputBuilder.AddCssClass("navbar navbar-expand-lg navbar-light bg-light nucleus-menu");

      TagBuilder divBuilder = new("div");
      divBuilder.AddCssClass("container-fluid");

      AddMainMenu(divBuilder, urlHelper, topMenu, menuStyle, maxLevels);
      AddMobileMenu(divBuilder, urlHelper, topMenu);

      outputBuilder.InnerHtml.AppendHtml(divBuilder);
      outputBuilder.MergeAttributes(htmlAttributes);      

			return outputBuilder;
		}

		private static void AddMainMenu(TagBuilder outputBuilder, IUrlHelper urlHelper, PageMenu topMenu, MenuStyles menuStyle, int maxLevels)
		{
			TagBuilder divInnerBuilder = new("div");
			TagBuilder listBuilder = new("ul");

			divInnerBuilder.AddCssClass("collapse navbar-collapse");

			listBuilder.AddCssClass("navbar-nav me-auto mb-2 mb-lg-0");
			AddChildren("nav-", menuStyle, listBuilder, urlHelper, topMenu, maxLevels, 0);

			divInnerBuilder.InnerHtml.AppendHtml(listBuilder);
			outputBuilder.InnerHtml.AppendHtml(divInnerBuilder);
		}

		private static void AddMobileMenu(TagBuilder outputBuilder, IUrlHelper urlHelper, PageMenu topMenu)
		{
			TagBuilder listBuilder = new("ul");
			listBuilder.AddCssClass("navbar-nav nucleus-mobile-menu d-lg-none d-xl-none d-xxl-none");

			AddChildren("nav-mobile-", MenuStyles.DropDown, listBuilder, urlHelper, topMenu, 1, 0);
			outputBuilder.InnerHtml.AppendHtml(listBuilder);
		}

		private static void AddChildren(string prefix, MenuStyles menuStyle, TagBuilder control, IUrlHelper urlHelper, PageMenu menu, int maxLevels, int thisLevel)
		{
			if (maxLevels != 0 && thisLevel == maxLevels) return;

			foreach (PageMenu childItem in menu.Children)
			{
				Boolean renderChildren = (maxLevels == 0 || (thisLevel + 1 < maxLevels)) && childItem.Children != null && childItem.Children.Any();
				TagBuilder itemBuilder = null;

				switch (menuStyle)
				{
					case MenuStyles.DropDown:
						itemBuilder = RenderDropDownItem(prefix, menuStyle, childItem, urlHelper, thisLevel, maxLevels, renderChildren);
						if (renderChildren)
						{
							itemBuilder.InnerHtml.AppendHtml(RenderDropDownChildren(prefix, menuStyle, childItem, urlHelper, thisLevel, maxLevels)); 
						}
						break;

					case MenuStyles.RibbonLandscape: 
					case MenuStyles.RibbonPortrait:
          case MenuStyles.Horizontal:
						if (thisLevel == 0)
						{
							// top level items are rendered the same as the default style
							itemBuilder = RenderDropDownItem(prefix, menuStyle, childItem, urlHelper, thisLevel, maxLevels, renderChildren);
						}
						else
						{
							itemBuilder = RenderRibbonItem(prefix, menuStyle, childItem, urlHelper, thisLevel, maxLevels, renderChildren);
						}
						if (renderChildren)
						{
							itemBuilder.InnerHtml.AppendHtml(RenderRibbonChildren(prefix, menuStyle, childItem, urlHelper, thisLevel, maxLevels));
						}
						break;											
				}

				if (itemBuilder != null)
				{
					control.InnerHtml.AppendHtml(itemBuilder);
				}
			}
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

    private static string GenerateControlId(Page page, string prefix)
		{
			PageRoute route = page.DefaultPageRoute();
			if (route?.Path == null) return "";
			return prefix + System.Text.RegularExpressions.Regex.Replace(route.Path.Replace(" ", "-"), @"([^0-9A-Za-z\.\-_])", "").ToLower();
		}

		private static TagBuilder RenderDropDownItem(string prefix, MenuStyles menuStyle, PageMenu childItem, IUrlHelper urlHelper, int thisLevel, int maxLevels, Boolean renderChildren)
		{
			TagBuilder itemBuilder = new("li");

			itemBuilder.Attributes.Add("id", GenerateControlId(childItem.Page, prefix));

			if (renderChildren)
			{
				itemBuilder.AddCssClass("dropdown-submenu");
			}
			else
			{
				itemBuilder.AddCssClass("nav-item");
			}
			
			string caption = String.IsNullOrWhiteSpace(childItem.Page.Title) ? childItem.Page.Name : childItem.Page.Title;
			TagBuilder linkBuilder = BuildLinkElement(childItem, urlHelper);
			
			if (thisLevel != 0)			
			{
				linkBuilder.AddCssClass("dropdown-item");
			}
			
			linkBuilder.InnerHtml.SetContent(caption);

			itemBuilder.InnerHtml.AppendHtml(linkBuilder);
			
			if (renderChildren)
			{
				// Tag the item as a dropdown-toggle control "keyboard only" control so that arrow keys work (handling code in in nucleus-shared.js)
				linkBuilder.Attributes.Add("data-bs-toggle", "dropdown-keyboardonly");

				// down arrow icon to expand display (show child pages)
				TagBuilder toggleLinkBuilder = new("button");
				toggleLinkBuilder.AddCssClass("dropdown-toggle nav-link d-inline-flex btn btn-none");
				toggleLinkBuilder.Attributes.Add("type", "button");
				toggleLinkBuilder.Attributes.Add("title", "Open");
				toggleLinkBuilder.Attributes.Add("aria-expanded", "false");
				toggleLinkBuilder.Attributes.Add("tabindex", "0");
				toggleLinkBuilder.Attributes.Add("data-bs-toggle", "dropdown");

				itemBuilder.InnerHtml.AppendHtml(toggleLinkBuilder);
			}
		

			return itemBuilder;
		}

		private static TagBuilder RenderDropDownChildren(string prefix, MenuStyles menuStyle, PageMenu childItem, IUrlHelper urlHelper, int thisLevel, int maxLevels)
		{			
			TagBuilder listBuilder = new("ul");
			listBuilder.AddCssClass("dropdown-menu");
			listBuilder.Attributes.Add("aria-live", "polite");
			AddChildren(prefix, menuStyle, listBuilder, urlHelper, childItem, maxLevels, thisLevel + 1);

			return listBuilder;
		}

		private static TagBuilder RenderRibbonItem(string prefix, MenuStyles menuStyle, PageMenu childItem, IUrlHelper urlHelper, int thisLevel, int maxLevels, Boolean renderChildren)
		{
			string caption = String.IsNullOrWhiteSpace(childItem.Page.Title) ? childItem.Page.Name : childItem.Page.Title;
			TagBuilder itemBuilder = new("li");

			itemBuilder.Attributes.Add("id", GenerateControlId(childItem.Page, prefix));

			TagBuilder linkBuilder = BuildLinkElement(childItem, urlHelper);

			if (thisLevel != 0)
			{
				linkBuilder.AddCssClass("dropdown-item");
			}

			linkBuilder.InnerHtml.SetContent(caption);

			itemBuilder.InnerHtml.AppendHtml(linkBuilder);

			return itemBuilder;
		}

		private static TagBuilder BuildLinkElement(PageMenu childItem, IUrlHelper urlHelper)
		{
			TagBuilder linkBuilder;

			if (!childItem.Page.DisableInMenu)
			{
				PageRoute defaultRoute = childItem.Page.DefaultPageRoute();
				if (defaultRoute == null)
				{
					linkBuilder = new("span");
					linkBuilder.AddCssClass("disabled");
				}
				else 
				{
					linkBuilder = new("a");
					linkBuilder.Attributes.Add("href", Nucleus.ViewFeatures.UrlHelperExtensions.PageLink(urlHelper, childItem.Page));
				}				
			}
			else
			{
				linkBuilder = new("button");
				linkBuilder.Attributes.Add("class", "btn btn-none");
				linkBuilder.Attributes.Add("type", "button");
				// for disabled items, apply disabled class if the item has no children, otherwise render as a LI/SPAN with no href
				if (!childItem.Children.Any())
				{
					linkBuilder.AddCssClass("disabled");
				}
			}

			if (childItem.Children.Any())
			{
				linkBuilder.AddCssClass("has-child-items");
			}

			linkBuilder.AddCssClass("nav-link");

			return linkBuilder;
		}

		private static TagBuilder RenderRibbonChildren(string prefix, MenuStyles menuStyle, PageMenu childItem, IUrlHelper urlHelper, int thisLevel, int maxLevels)
		{
			TagBuilder listBuilder = new("ul");
			
			if (thisLevel < 1)
			{
				listBuilder.AddCssClass("ribbon-item");
				listBuilder.AddCssClass(menuStyle.ToString());
				listBuilder.AddCssClass("dropdown-menu");
			}

			AddChildren(prefix, menuStyle, listBuilder, urlHelper, childItem, maxLevels, thisLevel + 1);

			return listBuilder;
		}

		
		
	}
}
