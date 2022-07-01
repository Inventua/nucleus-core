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

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// 
	/// </summary>
	public class Menu
	{
		/// <summary>
		/// Menu rendering styles
		/// </summary>
		public enum MenuStyles
		{
			/// <summary>
			/// Drop-down/Flyout style menu
			/// </summary>
			DropDown,
			/// <summary>
			/// Child items rendered horizontally as list blocks
			/// </summary>
			RibbonLandscape,
			/// <summary>
			/// Child items rendered vertically as list blocks
			/// </summary>
			RibbonPortrait,
		}

		internal static async Task<TagBuilder> Build(ViewContext context, MenuStyles menuStyle, int maxLevels, object htmlAttributes)
		{
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
			Site site = context.HttpContext.RequestServices.GetService<Context>().Site;
			IPageManager pageManager = context.HttpContext.RequestServices.GetService<IPageManager>();
			PageMenu topMenu = await pageManager.GetMenu
				(
					site,
					null,
					context.HttpContext.User,
					false
				);

			TagBuilder outputBuilder = new("nav");
			TagBuilder divBuilder = new("div");
			TagBuilder divInnerBuilder = new("div");
			TagBuilder listBuilder = new("ul");

			outputBuilder.AddCssClass("navbar navbar-expand-lg navbar-light bg-light nucleus-menu");
			divBuilder.AddCssClass("container-fluid");

			divInnerBuilder.AddCssClass("collapse navbar-collapse");

			listBuilder.AddCssClass("navbar-nav me-auto mb-2 mb-lg-0");
			AddChildren(menuStyle, listBuilder, urlHelper, topMenu, maxLevels, 0);

			outputBuilder.MergeAttributes(htmlAttributes);

			divInnerBuilder.InnerHtml.AppendHtml(listBuilder);
			divBuilder.InnerHtml.AppendHtml(divInnerBuilder);
			outputBuilder.InnerHtml.AppendHtml(divBuilder);

			return outputBuilder;
		}

		private static void AddChildren(MenuStyles menuStyle, TagBuilder control, IUrlHelper urlHelper, PageMenu menu, int maxLevels, int thisLevel)
		{
			if (maxLevels != 0 && thisLevel == maxLevels) return;

			foreach (PageMenu childItem in menu.Children)
			{
				Boolean renderChildren = (maxLevels == 0 || (thisLevel + 1 < maxLevels)) && childItem.Children != null && childItem.Children.Any();
				TagBuilder itemBuilder = null;

				switch (menuStyle)
				{
					case MenuStyles.DropDown:
						itemBuilder = RenderDropDownItem(menuStyle, childItem, urlHelper, thisLevel, maxLevels, renderChildren);
						if (renderChildren)
						{
							itemBuilder.InnerHtml.AppendHtml(RenderDropDownChildren(menuStyle, childItem, urlHelper, thisLevel, maxLevels)); 
						}
						break;

					case MenuStyles.RibbonLandscape: 
					case MenuStyles.RibbonPortrait:
						if (thisLevel == 0)
						{
							// top level items are rendered the same as the default style
							itemBuilder = RenderDropDownItem(menuStyle, childItem, urlHelper, thisLevel, maxLevels, renderChildren);
						}
						else
						{
							itemBuilder = RenderRibbonItem(menuStyle, childItem, urlHelper, thisLevel, maxLevels, renderChildren);
						}
						if (renderChildren)
						{
							itemBuilder.InnerHtml.AppendHtml(RenderRibbonChildren(menuStyle, childItem, urlHelper, thisLevel, maxLevels));
						}
						break;											
				}

				if (itemBuilder != null)
				{
					control.InnerHtml.AppendHtml(itemBuilder);
				}
			}
		}

		private static TagBuilder RenderDropDownItem(MenuStyles menuStyle, PageMenu childItem, IUrlHelper urlHelper, int thisLevel, int maxLevels, Boolean renderChildren)
		{
			TagBuilder itemBuilder = new("li");

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
				toggleLinkBuilder.Attributes.Add("title", "open");
				toggleLinkBuilder.Attributes.Add("role", "button");
				toggleLinkBuilder.Attributes.Add("aria-expanded", "false");
				toggleLinkBuilder.Attributes.Add("tabindex", "0");
				toggleLinkBuilder.Attributes.Add("data-bs-toggle", "dropdown");

				itemBuilder.InnerHtml.AppendHtml(toggleLinkBuilder);
			}
		

			return itemBuilder;
		}

		private static TagBuilder RenderDropDownChildren(MenuStyles menuStyle, PageMenu childItem, IUrlHelper urlHelper, int thisLevel, int maxLevels)
		{			
			TagBuilder listBuilder = new("ul");
			listBuilder.AddCssClass("dropdown-menu");
			listBuilder.Attributes.Add("aria-live", "polite");
			AddChildren(menuStyle, listBuilder, urlHelper, childItem, maxLevels, thisLevel + 1);

			return listBuilder;
		}

		private static TagBuilder RenderRibbonItem(MenuStyles menuStyle, PageMenu childItem, IUrlHelper urlHelper, int thisLevel, int maxLevels, Boolean renderChildren)
		{
			string caption = String.IsNullOrWhiteSpace(childItem.Page.Title) ? childItem.Page.Name : childItem.Page.Title;
			TagBuilder itemBuilder = new("li");
		
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

			linkBuilder.AddCssClass("nav-link");

			return linkBuilder;
		}

		private static TagBuilder RenderRibbonChildren(MenuStyles menuStyle, PageMenu childItem, IUrlHelper urlHelper, int thisLevel, int maxLevels)
		{
			TagBuilder listBuilder = new("ul");
			
			if (thisLevel < 1)
			{
				listBuilder.AddCssClass("ribbon-item");
				listBuilder.AddCssClass(menuStyle.ToString());
				listBuilder.AddCssClass("dropdown-menu");
			}

			AddChildren(menuStyle, listBuilder, urlHelper, childItem, maxLevels, thisLevel + 1);

			return listBuilder;
		}

		
		
	}
}
