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
using System.Linq;
using System.Threading.Tasks;

namespace Nucleus.ViewFeatures.HtmlContent
{
	/// <summary>
	/// Renders a PageMenu control.
	/// </summary>
	internal static class PageList
	{
		internal static async Task<TagBuilder> Build(ViewContext context, string propertyId, string propertyName, Guid selectedPageId, Guid? disabledPageId, Nucleus.Abstractions.Models.PageMenu pageMenu, string noSelectionText, object htmlAttributes)
		{
			TagBuilder outputBuilder = new("div");
			outputBuilder.AddCssClass("nucleus-page-list");
				
			IUrlHelper urlHelper = context.HttpContext.RequestServices.GetService<IUrlHelperFactory>().GetUrlHelper(context);
			//Context nucleusContext = context.HttpContext.RequestServices.GetService<Context>();
			IPageManager pageManager = context.HttpContext.RequestServices.GetService<IPageManager>();
			
			TagBuilder selectedIdBuilder = new("input");
			selectedIdBuilder.Attributes.Add("id",  propertyId);
			selectedIdBuilder.Attributes.Add("name", propertyName);
			selectedIdBuilder.Attributes.Add("value", selectedPageId.ToString());
			selectedIdBuilder.Attributes.Add("type", "hidden");			
			outputBuilder.InnerHtml.AppendHtml(selectedIdBuilder);

			TagBuilder selectedItemBuilder = new("div");
			selectedItemBuilder.AddCssClass("nucleus-page-list-selected");
			selectedItemBuilder.InnerHtml.SetContent(selectedPageId==Guid.Empty ? noSelectionText : (await pageManager.Get(selectedPageId))?.Name);
			outputBuilder.InnerHtml.AppendHtml(selectedItemBuilder);

			TagBuilder listBuilder = new("ul");
			listBuilder.AddCssClass("collapse");
			AddChildren(urlHelper, listBuilder, pageMenu, selectedPageId, disabledPageId, noSelectionText, 1, 0, htmlAttributes);
			listBuilder.MergeAttributes(htmlAttributes);
			outputBuilder.InnerHtml.AppendHtml(listBuilder);

			TagBuilder buttonBuilder = new("button");
			buttonBuilder.AddCssClass("nucleus-material-icon");
			buttonBuilder.AddCssClass("btn btn-primary rounded-0 py-0 px-1");
			buttonBuilder.Attributes.Add("type", "button");
			buttonBuilder.InnerHtml.SetHtmlContent("&#xe5cf;");
			outputBuilder.InnerHtml.AppendHtml(buttonBuilder);

			return outputBuilder;
		}

		private static void AddChildren(IUrlHelper urlHelper, TagBuilder control, PageMenu menu, Guid selectedPageId, Guid? disabledPageId, string noSelectionText, int maxLevels, int thisLevel, object htmlAttributes)
		{
			if (thisLevel == maxLevels) return;

			if (thisLevel == 0)
			{
				TagBuilder itemBuilder = new("li");

				TagBuilder linkBuilder = new("a");
				linkBuilder.Attributes.Add("data-id", Guid.Empty.ToString());
				linkBuilder.Attributes.Add("tabindex", "0");
				linkBuilder.InnerHtml.SetContent(noSelectionText);
				itemBuilder.InnerHtml.AppendHtml(linkBuilder);
				itemBuilder.MergeAttributes(htmlAttributes);
				control.InnerHtml.AppendHtml(itemBuilder);
			}

			foreach (PageMenu childItem in menu.Children)
			{
				TagBuilder itemBuilder = new("li");

				if (childItem.Page.Id == selectedPageId)
				{
					itemBuilder.AddCssClass("selected");
				}

				string caption = !String.IsNullOrWhiteSpace(childItem.Page.Title) ? childItem.Page.Title : childItem.Page.Name;
					
				if (childItem.Page.Id == disabledPageId)
				{
					TagBuilder disabledItemBuilder = new("span");
					disabledItemBuilder.InnerHtml.SetContent(caption);
					disabledItemBuilder.AddCssClass("text-muted");
					
					itemBuilder.InnerHtml.AppendHtml(disabledItemBuilder);
				}
				else
				{
					TagBuilder linkBuilder = new("a");
					linkBuilder.Attributes.Add("data-id", childItem.Page.Id.ToString());
					linkBuilder.Attributes.Add("data-linkurl", urlHelper.PageLink(childItem.Page));
					linkBuilder.Attributes.Add("tabindex", "0");

					linkBuilder.InnerHtml.SetContent(caption);

					itemBuilder.InnerHtml.AppendHtml(linkBuilder);
				}				

				itemBuilder.MergeAttributes(htmlAttributes);

				control.InnerHtml.AppendHtml(itemBuilder);

				if (childItem.HasChildren && childItem.Page.Id != disabledPageId)
				{
					itemBuilder.InnerHtml.AppendHtml(HtmlHelpers.PageMenuHtmlHelper.RenderExpandButton(childItem.Page, true));
				}

				if (childItem.Children != null && childItem.Children.Any())
				{
					TagBuilder childList = new("ul");
					AddChildren(urlHelper, childList, childItem, selectedPageId, disabledPageId, noSelectionText, maxLevels, thisLevel + 1, htmlAttributes);
					itemBuilder.InnerHtml.AppendHtml(childList);
				}
			}
		}
	}
}


