using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Nucleus.ViewFeatures.HtmlHelpers
{
	/// <summary>
	/// Html helper used to render a list of pages.
	/// </summary>
	public static class PageMenuHtmlHelper
	{
		/// <summary>
		/// Returns a list (<![CDATA[ul]]>) element containing the site's menu structure.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="pages"></param>
		/// <param name="selectedPageId"></param>
		/// <param name="maxLevels"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent PageMenu(this IHtmlHelper htmlHelper, PageMenu pages, Guid selectedPageId, int maxLevels, object htmlAttributes)
		{
			IUrlHelper urlHelper = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(htmlHelper.ViewContext);
			TagBuilder outputBuilder = new("ul");

			AddChildren(urlHelper, outputBuilder, pages, selectedPageId, maxLevels, 0, htmlAttributes);

			return outputBuilder;
		}

		/// <summary>
		/// Render the 'a' tag for the specified parentPage, along with its children.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="pages"></param>
		/// <param name="fromPage"></param>
		/// <param name="maxLevels"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		/// <remarks>
		/// This helper is used by the 'Pages' index to expand child page selections. 
		/// </remarks>
		public static IHtmlContent PageMenuFor(this IHtmlHelper htmlHelper, PageMenu pages, Page fromPage, int maxLevels, object htmlAttributes)
		{
			//string itemUrl;
			IUrlHelper urlHelper = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(htmlHelper.ViewContext);
			HtmlContentBuilder outputBuilder = new();

			TagBuilder linkBuilder = new("a");
			string caption = !String.IsNullOrWhiteSpace(fromPage.Title) ? fromPage.Title : fromPage.Name;

			linkBuilder.Attributes.Add("data-id", fromPage.Id.ToString());
			linkBuilder.Attributes.Add("data-linkurl", urlHelper.PageLink(fromPage));
			linkBuilder.Attributes.Add("tabindex", "0");
			linkBuilder.InnerHtml.SetContent(caption);

			outputBuilder.AppendHtml(linkBuilder);
						
			outputBuilder.AppendHtml(RenderExpandButton(fromPage, false));
			outputBuilder.AppendHtml(RenderCollapseButton(fromPage, true));

			TagBuilder listBuilder = new("ul");
			AddChildren(urlHelper, listBuilder, pages, Guid.Empty, maxLevels, 0, htmlAttributes);
			outputBuilder.AppendHtml(listBuilder);

			return outputBuilder;
		}

		private static Boolean IsInTree(PageMenu menu, Guid selectedPageId)
		{
			if (menu.Children != null && menu.Children?.Any() == true)
			{
				foreach (PageMenu child in menu.Children)
				{
					if (child.Page.Id == selectedPageId)
					{
						return true;
					}
					else
					{
						if (IsInTree(child, selectedPageId)) return true;
					}
				}
			}
			
			return false;			
		}

		private static void AddChildren(IUrlHelper urlHelper, TagBuilder control, PageMenu menu, Guid selectedPageId, int maxLevels, int thisLevel, object htmlAttributes)
		{
			if (thisLevel == maxLevels && !IsInTree(menu, selectedPageId)) return;

			if (menu.HasChildren && menu.Children?.Any() == true)
			{
				foreach (PageMenu childItem in menu.Children)
				{
					TagBuilder itemBuilder = new("li");

					if (childItem.Page.Id == selectedPageId)
					{
						itemBuilder.AddCssClass("selected");
					}

					string caption = !String.IsNullOrWhiteSpace(childItem.Page.Title) ? childItem.Page.Title : childItem.Page.Name;

					TagBuilder linkBuilder = new("a");
					// We append a "/" so that if the path contains dots the net core static file provider doesn't interpret the path as a file
					//itemUrl = url.Replace("{id}", childItem.Page.Id.ToString());
					//linkBuilder.Attributes.Add("href", itemUrl);
					linkBuilder.Attributes.Add("tabindex", "0");
					linkBuilder.Attributes.Add("data-id", childItem.Page.Id.ToString());
					linkBuilder.Attributes.Add("data-linkurl", urlHelper.PageLink(childItem.Page));
					linkBuilder.InnerHtml.SetContent(caption);

					itemBuilder.InnerHtml.AppendHtml(linkBuilder);

					itemBuilder.MergeAttributes(htmlAttributes);

					control.InnerHtml.AppendHtml(itemBuilder);

					if (childItem.HasChildren)
					{
						itemBuilder.InnerHtml.AppendHtml(RenderExpandButton(childItem.Page, true));
					}

					if (childItem.Children != null && childItem.Children?.Any()==true)
					{
						TagBuilder childList = new("ul");
						AddChildren(urlHelper, childList, childItem, selectedPageId, maxLevels, thisLevel + 1, htmlAttributes);
						itemBuilder.InnerHtml.AppendHtml(childList);
					}
				}
			}
		}

		internal static TagBuilder RenderExpandButton(Page page, Boolean show)
		{
			TagBuilder expandlinkBuilder = new("button");
			expandlinkBuilder.AddCssClass($"btn btn-none nucleus-get-childpages nucleus-material-icon collapse{(show ? " show" : "")}");
			expandlinkBuilder.Attributes.Add("type", "button");
			expandlinkBuilder.Attributes.Add("data-id", page.Id.ToString());
			expandlinkBuilder.Attributes.Add("tabindex", "0");
			expandlinkBuilder.InnerHtml.SetHtmlContent("&#xe5cc;");

			return expandlinkBuilder;
		}

		internal static TagBuilder RenderCollapseButton(Page page, Boolean show)
		{
			TagBuilder collapselinkBuilder = new("button");
			collapselinkBuilder.AddCssClass($"btn btn-none nucleus-hide-childpages nucleus-material-icon collapse{(show ? " show" : "")}");
			collapselinkBuilder.Attributes.Add("type", "button");
			collapselinkBuilder.Attributes.Add("data-id", page.Id.ToString());
			collapselinkBuilder.Attributes.Add("tabindex", "0");
			collapselinkBuilder.InnerHtml.SetHtmlContent("&#xe5cf;");

			return collapselinkBuilder;
		}
	}
}
