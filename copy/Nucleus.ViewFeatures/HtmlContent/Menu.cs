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
using Nucleus.Core.Authorization;
using Nucleus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Nucleus.ViewFeatures.HtmlContent
{
	public class Menu
	{
		internal static TagBuilder Build(ViewContext context, int maxLevels, object htmlAttributes)
		{
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
			Site site = context.HttpContext.RequestServices.GetService<Context>().Site;
			PageMenu topMenu = context.HttpContext.RequestServices.GetService<PageManager>().GetMenu
				(
					site,
					null,
					context.HttpContext.User,
					false
				);

			TagBuilder outputBuilder = new TagBuilder("nav");
			TagBuilder divBuilder = new TagBuilder("div");
			TagBuilder divInnerBuilder = new TagBuilder("div");
			TagBuilder listBuilder = new TagBuilder("ul");

			outputBuilder.AddCssClass("navbar navbar-expand-lg navbar-light bg-light");
			divBuilder.AddCssClass("container-fluid");

			divInnerBuilder.AddCssClass("collapse navbar-collapse");

			listBuilder.AddCssClass("navbar-nav me-auto mb-2 mb-lg-0");
			AddChildren(listBuilder, urlHelper, topMenu, maxLevels, 0);

			outputBuilder.MergeAttributes(htmlAttributes);

			divInnerBuilder.InnerHtml.AppendHtml(listBuilder);
			divBuilder.InnerHtml.AppendHtml(divInnerBuilder);
			outputBuilder.InnerHtml.AppendHtml(divBuilder);

			return outputBuilder;
		}

		private static void AddChildren(TagBuilder control, IUrlHelper urlHelper, PageMenu menu, int maxLevels, int thisLevel)
		{
			if (maxLevels != 0 && thisLevel == maxLevels) return;

			foreach (PageMenu childItem in menu.Children)
			{
				TagBuilder itemBuilder = new TagBuilder("li");
				if (childItem.Children != null && childItem.Children.Any())
				{
					itemBuilder.AddCssClass("dropdown-submenu");
				}
				else
				{
					itemBuilder.AddCssClass("nav-item");
				}
				string caption = !String.IsNullOrWhiteSpace(childItem.Page.Title) ? childItem.Page.Title : childItem.Page.Name;
				PageRoute defaultRoute = null;

				if (!childItem.Page.DisableInMenu)
				{
					defaultRoute = childItem.Page.DefaultPageRoute();
				}

				TagBuilder linkBuilder = new TagBuilder("a");
								
				linkBuilder.AddCssClass("nav-link");

				if (thisLevel != 0)
				{
					linkBuilder.AddCssClass("dropdown-item");
				}
				linkBuilder.InnerHtml.SetContent(caption);

				if (defaultRoute == null)
				{
					linkBuilder.AddCssClass("disabled");
				}
				else
				{
					// We append a "/" so that if the path contains dots the net core static file provider doesn't interpret the path as a file
					//linkBuilder.Attributes.Add("href", defaultRoute.Path + (defaultRoute.Path.EndsWith("/") ? "" : "/"));
					linkBuilder.Attributes.Add("href", Nucleus.ViewFeatures.UrlHelperExtensions.PageLink(urlHelper, childItem.Page));
				}

				itemBuilder.InnerHtml.AppendHtml(linkBuilder);

				if (childItem.Children != null && childItem.Children.Any())
				{
					TagBuilder dropDownBuilder = new TagBuilder("a");

					dropDownBuilder.AddCssClass("dropdown-toggle");
					dropDownBuilder.Attributes.Add("style", "display: inline-flex;");
					dropDownBuilder.Attributes.Add("title", "open");
					dropDownBuilder.Attributes.Add("role", "button");
					dropDownBuilder.Attributes.Add("data-bs-toggle", "dropdown");
					dropDownBuilder.Attributes.Add("aria-expanded", "false");

					itemBuilder.InnerHtml.AppendHtml(dropDownBuilder);
				}


				control.InnerHtml.AppendHtml(itemBuilder);

				if (childItem.Children != null && childItem.Children.Any())
				{
					TagBuilder childList = new TagBuilder("ul");
					childList.AddCssClass("dropdown-menu");
					AddChildren(childList, urlHelper, childItem, maxLevels, thisLevel + 1);
					itemBuilder.InnerHtml.AppendHtml(childList);
				}
			}
		}
	}
}
