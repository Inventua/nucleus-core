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
using System.Linq.Expressions;

namespace Nucleus.ViewFeatures.HtmlHelpers
{
	public static class PageSelectorHtmlHelper
	{
		//public static IHtmlContent PageSelectorFor<TModel, TResult>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TResult>> expression, PageMenu pages, int maxLevels)
		//{
		//	return PageSelectorFor(htmlHelper, expression, pages, maxLevels, null);
		//}

		/// <summary>
		/// Returns a dropdown list element containing the site's menu structure.
		/// </summary>
		/// <param name="htmlHelper"></param>
		/// <param name="htmlAttributes"></param>
		/// <returns></returns>
		public static IHtmlContent PageSelectorFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, PageMenu pages, int maxLevels, object htmlAttributes)
		{
			Guid selectedPageId = Guid.Parse(htmlHelper.ValueFor(expression));
			
			TagBuilder outputBuilder = new TagBuilder("select");
			outputBuilder.Attributes.Add("id", htmlHelper.IdFor(expression));
			outputBuilder.Attributes.Add("name", htmlHelper.NameFor(expression));
			
			AddChildren(outputBuilder, pages, selectedPageId, maxLevels, 0, htmlAttributes);
			outputBuilder.MergeAttributes(htmlAttributes);

			return outputBuilder;
		}

		private static void AddChildren(TagBuilder control, PageMenu menu, Guid selectedPageId, int maxLevels, int thisLevel, object htmlAttributes)
		{
			if (thisLevel == maxLevels) return;

			foreach (PageMenu childItem in menu.Children)
			{
				TagBuilder itemBuilder = new TagBuilder("option");
				
				if (childItem.Page.Id == selectedPageId)
				{
					itemBuilder.Attributes.Add("selected", "selected");
				}

				string prefix = string.Join("", Enumerable.Repeat("&nbsp;", thisLevel * 2));
				string caption = new string(' ', thisLevel *2) + (!String.IsNullOrWhiteSpace(childItem.Page.Title) ? childItem.Page.Title : childItem.Page.Name);
				
				itemBuilder.InnerHtml.SetHtmlContent(prefix + caption);

				control.InnerHtml.AppendHtml(itemBuilder);

				if (childItem.Children != null && childItem.Children.Any())
				{
					AddChildren(control, childItem, selectedPageId, maxLevels, thisLevel + 1, htmlAttributes);					
				}
			}
		}
	}
}
