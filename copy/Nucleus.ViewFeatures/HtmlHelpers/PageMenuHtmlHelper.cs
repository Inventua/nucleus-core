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

namespace Nucleus.ViewFeatures.HtmlHelpers
{
	public static class PageMenuHtmlHelper
	{
		/// <summary>
		/// Returns a list (<![CDATA[ul]]>) element containing the site's menu structure.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent PageMenu(this IHtmlHelper htmlHelper, PageMenu pages, string url, Guid SelectedPageId, int maxLevels, object htmlAttributes)
		{
			TagBuilder outputBuilder = new TagBuilder("ul");

			AddChildren(outputBuilder, pages, url, SelectedPageId, maxLevels, 0, htmlAttributes);
						
			return outputBuilder;
		}

		private static void AddChildren(TagBuilder control, PageMenu menu, string url, Guid selectedPageId, int maxLevels, int thisLevel, object htmlAttributes)
		{
			string itemUrl;

			if (thisLevel == maxLevels) return;

			foreach (PageMenu childItem in menu.Children)
			{
				TagBuilder itemBuilder = new TagBuilder("li");
				
				if (childItem.Page.Id == selectedPageId)
				{
					itemBuilder.AddCssClass("selected");
				}
								
				string caption = !String.IsNullOrWhiteSpace(childItem.Page.Title) ? childItem.Page.Title : childItem.Page.Name;
				
				TagBuilder linkBuilder = new TagBuilder("a");
				// We append a "/" so that if the path contains dots the net core static file provider doesn't interpret the path as a file
				itemUrl = url.Replace("{id}", childItem.Page.Id.ToString());
				linkBuilder.Attributes.Add("href", itemUrl);

				linkBuilder.InnerHtml.SetContent(caption);

				itemBuilder.InnerHtml.AppendHtml(linkBuilder);
				

				itemBuilder.MergeAttributes(htmlAttributes);

				control.InnerHtml.AppendHtml(itemBuilder);

				if (childItem.Children != null && childItem.Children.Any())
				{
					TagBuilder childList = new TagBuilder("ul");
					AddChildren(childList, childItem, url, selectedPageId, maxLevels, thisLevel + 1, htmlAttributes);
					itemBuilder.InnerHtml.AppendHtml(childList);
				}
			}
		}
	}
}
